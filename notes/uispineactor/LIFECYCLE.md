# LIFECYCLE — `ActionSystem.UISpineActor`

เอกสารนี้สรุปลำดับชีวิตของ actor, จุดที่ object ถูกสร้าง/ทำลาย, ownership ของ resource,
และ timing hazard ที่ทำให้พฤติกรรมต่างจากการเรียกแบบ synchronous

สถานะ: **Observed** จาก decompile; ข้อที่เป็นการสรุปจะระบุ **Derived**

---

## 1. State machine

```
                 AddComponent<UISpineActor>
                            │
                            ▼
                       [Created]  (Awake: ModelRoot = transform)
                            │
             LoadBody / GUILoadBody
                            │
                            ▼
                    [LoadingBody]  ── AB async (SkinReq)
                            │
              ┌─────────────┴─────────────┐
              │ body ok                    │ body fail
              ▼                            ▼
      [LoadingAttachSkins]            [Failed]
              │  (ถ้ามี attach)
              ▼
          [Building]  ── BuildBody()
              │
              ▼
           [Ready]  ◄──────────── SetTarget / LoadBody ซ้ำ
              │                        (CancelAllReq → LoadingBody)
              │
              │ OnRecycle / RemoveTarget
              ▼
         [Recycled] ──(RemoveTarget)──► GameObject destroyed
              │
              └── LoadBody อีกครั้ง → [LoadingBody]
```

### 1.1 สิ่งที่เกิดในแต่ละ state

| State | สิ่งที่พร้อม | สิ่งที่ยัง null |
|---|---|---|
| Created | `transform`, `ModelRoot` | `m_RootTransform`, SG, skin, animation |
| LoadingBody | `Root` + `SortingGroup` (CreateRoot) | SG, `m_ISkeleton`, animation |
| Building | SG ถูกสร้างแล้ว (ใน `CreateSpineComponent`) | sub/follow บางส่วนอาจยังไม่เสร็จ |
| Ready | SG + skin + sub + follow + animation | — |
| Recycled | `Root` ยังอยู่, SG ถูก destroy | SG, `m_ISkeleton`, animation |
| Destroyed | — | ทั้ง GameObject |

> **สำคัญ:** `RecycleSelf` **ไม่ destroy** `Root`/`SortingGroup`/`m_MountActor`
> (mount แค่ deactivate) → actor ถูก reuse ได้; `RemoveTarget` ของ host จึงต้อง
> `Destroy(m_ActorGameObject)` เพื่อเก็บกวาดทั้งหมด

---

## 2. ลำดับ create → ready (ละเอียด)

### 2.1 ตอน `AddComponent`

```
Awake()
  └─ ModelRoot = transform
```

ยังไม่มี `Start()` ใน `SpineActor`; ไม่มีการสร้าง renderer ใน `Awake`

### 2.2 `SetTarget` ของ host (เส้นทาง UI หลัก)

```
UIModelShow.SetTarget(...)
  1. CreateActor(modelID)
       ├─ InitSoftMask()          (ถ้ามี RawImage)
       ├─ RemoveTarget()          (ของเก่า: OnRecycle + Destroy)
       ├─ IsSpineBody?
       ├─ new GameObject("SpineActor") + AddComponent<UISpineActor>()
       └─ SetAsFirstSibling()
  2. InitUISpinePara(...)          → 3 setters (ยังไม่มี SG)
  3. m_OrthographicSizeScale = 1/scale
  4. callback เก่า (m_OnLoadFinish) ถ้ามี
  5. FlushHUDTarget()              (GetHudOffset ยังเป็น 0)
  6. ถ้า !stopAnim: SetAnimationState(animID)
       └─ base บันทึก m_StateID/m_AnimationSpeed **ก่อน** ตรวจ m_IAnimationState
          (ตอนนี้ null → ไม่เล่น แต่ state ถูกจำไว้)
  7. GUILoadBody(modelID, comps, scale, softMask, fadeIn, callback)
       └─ LoadBody(isGUI:true) → CreateRoot → CancelAllReq → StartLoadMain
  8. SetSortingLayer("Default")
```

### 2.3 `LoadBody` (base)

```
LoadBody(bodyID, slots, isGUI, scale, isSoftClip, onLoadFinish, small)
  ├─ m_Scale = scale ; m_IsSmall = small
  ├─ callback เก่า (m_OnLoadFinish) ถ้ามี
  ├─ m_OnLoadFinish = onLoadFinish
  ├─ CreateRoot()
  │    └─ ถ้า m_RootTransform == null:
  │         new GameObject("Root") + SortingGroup + ComputerSortIndex
  ├─ CancelAllReq()
  │    ├─ ReleaseSkinReq()           (คืน/ยกเลิก body + attach)
  │    ├─ CanelAllImageDisplayReq()
  │    ├─ CancelRelaceAttachment()
  │    └─ ReleaseFollowActor()       (RecycleSelf + Destroy GO)
  └─ StartLoadMain(bodyID, slots)
       ├─ GetDBBodyConfig
       ├─ width/height = size × scale (หรือ scale)
       ├─ Root.localScale = scale
       ├─ FlushHudOffset()           ◄── UI: ×125
       ├─ เลือก spineAb/s_spineAb ตาม m_IsSmall
       ├─ new SkinReq(...) + attach reqs (ถ้า !noCmp)
       └─ m_BodySkinReq.Start()      ◄── async
```

**หมายเหตุ:** `LoadBody` ไม่ destroy SG เก่า; SG เก่าจะถูก destroy ตอน
`BuildBody` → `CreateSpineComponent` → `DestroySkeleton`
→ ระหว่างรอ AB ภาพเก่ายังแสดงอยู่ (Derived)

### 2.4 เมื่อ body โหลดเสร็จ

```
SkinReq.OnLoaded(data)
  ├─ data.Retain()
  └─ OnBodyLoadFinish(skinReq)
       ├─ data != null → StartLoadAttachSkins()
       │    ├─ ไม่มี attach → BuildBody()
       │    └─ มี → start ทุก attach req
       └─ data == null → m_OnLoadFinish?.Invoke() + LogError
```

### 2.5 `BuildBody` (base) — จุดที่ UI hook ถูกเรียก

```
BuildBody()
  1. CreateSpineComponent(bodyskin.skeletonDataAsset)     ◄── virtual (UI)
       ├─ DestroySkeleton()                               ◄── virtual (UI)
       ├─ CreateSkeletonGraphic(data, m_Gray, m_TintBlack)
       ├─ updateWhenInvisible = m_InvisbleUpdateMode
       ├─ ผูก m_ISkeleton/m_IAnimationState/m_SkeletonTransform
       └─ m_SortingGroup.enabled = false
  2. ถ้า m_BodyCfg.overrideSpine → SkeletonUtility.SpawnHierarchy
  3. ถ้าไม่มี attach:
       CreateSkeletonSkinWithNoCmp(m_ISkeleton, SkinNames)
     ไม่งั้น:
       BuildSlotMap()
       รวม skin หลัก
       ผูก AnimationState.Event (ถ้ามี events)
       สำหรับ attach req:
         ├─ มี followSlot + cmpRootBone → LoadSkeletonAttachment → CreateSkeletonActor/BuildCmpBody
         ├─ มี followSlot ไม่มี cmpRootBone → เก็บเข้า list
         └─ ไม่มี followSlot → AddSkin
       SetSkin(base_skin)
       ถ้ามี list → CreateSubSkeleton(list)                ◄── virtual (UI)
  4. fade-in setup (m_FadeInAlpha)
  5. m_SkeletonTransform.localScale = flip
  6. SetAnimationState(m_StateID, m_AnimationSpeed)
       └─ UpdateAnimationState(...)                       ◄── virtual (UI)
  7. SetFreezeAnimation(m_FreezeAnimation)                 ◄── virtual; UI no-op
  8. DoUpdateColor()                                       ◄── virtual (UI)
  9. FlushShadow()
 10. ReplaySlotEffect() → PlaySlotEffect(...)              ◄── virtual (UI)
 11. StartLoadImageDisplay()
 12. StartLoadAttachment()
 13. SetGray(m_Gray)                                      ◄── virtual (UI)
 14. m_OnLoadFinish?.Invoke()
 15. SetupSpine()                                          ◄── virtual (UI)
       ├─ UpdateRectTranformSize()
       ├─ ถ้ามี attach skin → allowMultipleCanvasRenderers = true
       ├─ DoHideSlots()
       └─ SetFlipX(m_FlipX)
```

### 2.6 หลังพร้อม

```
UIModelShow.OnLoadBodyFinish
  ├─ m_OnLoadFinish?.Invoke() (แล้ว set null)
  ├─ FlushHUDTarget()
  ├─ InitFollow()          (spine: no-op)
  └─ GenRenderTexure()     (UI DrawToBuffer ว่าง)
```

---

## 3. Update loop

```
Unity frame
├─ SpineActor.Update()
│    ถ้า m_ISkeleton != null:
│    ├─ UpdateMount()            ◄── UI: bone × referencePixelsPerUnit
│    ├─ m_Slash.Update(dt) → FlushSlashProperty()   ◄── UI: LogError
│    └─ UpdateColor(dt) → DoUpdateColor()           ◄── UI: .color
│
└─ SkeletonGraphic.Update()      (MonoBehaviour ของ SG เอง)
     └─ update ตาม updateMode/updateWhenInvisible
```

- `SkeletonGraphic` มี update ของตัวเอง; `ForceUpdate` เป็นการเรียก manual
  (advance 1 frame + rebuild) นอก loop
- `UpdateMount` ทำงานเฉพาะเมื่อ `m_IsMounting == true` และ `m_MountHpTransform != null`
- `FlushSlashProperty` ถูกเรียกเมื่อ lerp ของ `m_Slash` จบ (ปกติ `m_Slash` เริ่ม 1→1)
  → UI อาจ log error ซ้ำได้ถ้ามีการเรียก `Twinkle`

---

## 4. Recycle / destroy

### 4.1 `OnRecycle` → `RecycleSelf` (UI)

```
SpineActor.OnRecycle()
  └─ RecycleSelf()                         ◄── virtual (UI)
       ├─ base.RecycleSelf()
       │    ├─ UnMount()
       │    │    ├─ m_MountActor?.OnRecycle()        (mount RecycleSelf)
       │    │    └─ ถ้า mounting: false + DoUnMount + mount.SetActive(false)
       │    ├─ CleanTweener()
       │    ├─ CancelAllReq()
       │    │    ├─ ReleaseSkinReq()   (body + attach; AB refcount)
       │    │    ├─ CanelAllImageDisplayReq()
       │    │    ├─ CancelRelaceAttachment()
       │    │    └─ ReleaseFollowActor() (follow.RecycleSelf + Destroy GO)
       │    ├─ reset m_Scale/m_Flip/m_OnLoadFinish/m_Width/m_Height/m_Gray/
       │    │        m_EnableRayHit/m_ABLoadPriority/m_BodyCfg/
       │    │        m_Separator*/m_HideSlotNames/m_InvisbleUpdateMode
       │    ├─ transform.localRotation/Position/Scale reset
       │    ├─ Root.localPosition = zero ; SortingGroup.sortingOrder = 0
       │    ├─ color/fade/slash reset
       │    ├─ DestroySkeleton()                     ◄── virtual (UI)
       │    ├─ m_StateID=0, m_LeaveStateID=-1, m_AnimationSpeed=1,
       │    │   m_FreezeAnimation=false, ztest/queue/shadow/simLight reset
       │    ├─ StopSlotEffect(); RemoveEffectHandle(); clear SlotEffectInfo
       │    ├─ m_FollowerTransform.Clear()
       │    ├─ ReleaseStateFinishEvent()
       │    ├─ ReleaseFollowActor()   (ครั้งที่ 2 — list ว่างแล้ว)
       │    └─ m_CullableObject?.ClearCulling(true)
       └─ UI reset:
            m_useClipping = false
            m_updateRectTranformSize = false
            m_CanvasGroupCompatible = true
            m_PMAVertexColor = true
            m_TintBlack = false
```

**Derived:** หลัง recycle actor ยังมี `Root` + `SortingGroup` + `SpineMount` (inactive)
และ `transform` เดิม; host จึงต้อง destroy GameObject เองถ้าไม่ reuse

### 4.2 `RemoveTarget` (host)

```
UIModelShow.RemoveTarget()
  ├─ m_ActorTarget.OnRecycle()
  ├─ Destroy(m_ActorGameObject)
  ├─ m_ActorTarget = null
  ├─ m_ActorGameObject = null
  └─ m_EnableFollow = false
```

`OnDestroy` ของ `SpineActor` → `UnregsterLight()` (UI ไม่มี mesh → no-op)

### 4.3 Ownership matrix

| Resource | สร้างโดย | ทำลาย/คืนโดย | หมายเหตุ |
|---|---|---|---|
| `Root` + `SortingGroup` | `CreateRoot` (base) | ไม่มี (อยู่จน GameObject ถูก destroy) | reuse ได้ |
| `SkeletonGraphic` (main) | `CreateSkeletonGraphic` | `DestroySkeleton` → `Destroy(gameObject)` | |
| sub-`SkeletonGraphic` | `CreateSubSkeletonGraphic` | `DestroySkeleton` | |
| `SepSGRoot` | `CreateSubSkeleton` | `DestroySkeleton` | |
| separator parts | `SkeletonGraphic` (fork) | `DestroySkeleton` คืนเข้า SG ก่อน destroy | UI ไม่ destroy เอง |
| `SpineMount` + mount SG | `CreateMountActor` + mount `LoadBody` | `UnMount` แค่ recycle + deactivate; `DestroySkeleton` ของ mount เมื่อ recycle | ไม่ destroy ตอน unmount |
| follow actor GO | `CreateSkeletonActor` | `ReleaseFollowActor` → `RecycleSelf` + `Destroy` | |
| `BoneFollowerGraphic` GO | `GetBoneFollowerTransform`/`CreateFollow` | ถูก destroy ตาม SG/GO parent | cache ถูกล้างใน recycle |
| slot effect | `EffectSystemLocator` | `StopSlotEffect`/`RemoveEffectHandle` | handle เก็บใน `SlotEffectInfo` |
| material 8 ตัว | `GUIConfig` (asset) | **ห้าม destroy** | shared |
| RenderTexture / CommandBuffer | `UIModelShow` | `ReleaseSoftMask`/host | ไม่ใช่ของ actor |
| `SkinReq` / `SkeletionDataAssetRef` | `LoadBody`/`StartLoadMain` | `CancelAllReq` → `ReleaseSkinReq` | refcount |
| `ReplaceAttachmentReq` | `ChangeSlotDisplay` | `CancelRelaceAttachment`/`Replace` | |
| `RegionAttachmentReq` (image) | `ChangeSlotImage` | `CanelAllImageDisplayReq` | |

### 4.4 Cleanup checklist สำหรับ clone

- [ ] `DestroySkeleton` คืน separator parts เข้า SG **ก่อน** destroy SG
- [ ] destroy sub-SG ทั้งหมด + clear list
- [ ] destroy SG GameObject + set field เป็น null
- [ ] destroy `SepSGRoot` + set null
- [ ] clear slot map
- [ ] clear follower transform cache ตอน recycle
- [ ] stop effect ทุก handle
- [ ] release AB refcount (body + attach) และ cancel ที่ยังโหลด
- [ ] follow actor: recycle + destroy
- [ ] mount: recycle + deactivate (ไม่ destroy) ตอน unmount
- [ ] **ไม่** destroy material ที่มาจาก config
- [ ] **ไม่** destroy `Root` ถ้าจะ reuse
- [ ] `ForceUpdate`/`SetUseClipping`/`SetUpdateMode` ต้อง no-op ปลอดภัยเมื่อยังไม่มี SG

---

## 5. Mount lifecycle

```
Mount()
  ├─ CreateMountActor()
  │    ├─ (ครั้งแรก) GameObject "SpineMount" + UISpineActor
  │    └─ CreateRoot + copy flip/sorting + ComputerSortIndex
  ├─ เก็บ m_MountHP/m_MountOffset/m_OnMountFinish
  ├─ mount.LoadBody(..., OnMountLoadFinish)   [async]
  └─ mount.SeparatorSkeleton(...)             [มัก fail → fallback sibling]

OnMountLoadFinish()  [เมื่อ mount โหลดเสร็จ]
  ├─ ถ้า m_IsMounting อยู่ → DoUnMount() (เคลียร์ของเก่า)
  ├─ m_IsMounting = true
  ├─ mount.SetActive(true)
  ├─ mount.SetFlipX/Y(owner)
  ├─ mount.SetUpdateMode(owner.m_InvisbleUpdateMode)
  ├─ DoMount()
  │    ├─ ถ้า m_MountHP ว่าง → m_MountHpTransform = mount.Root
  │    ├─ ไม่งั้น → m_HpBone = mount.Skeleton.FindBone(m_MountHP)
  │    │              m_MountHpTransform = mount.Root
  │    └─ mount.SeparatorSkeleton(owner.m_SeparatorSlotNames, ...)
  └─ m_OnMountFinish?.Invoke()

UpdateMount()  [ทุกเฟรมเมื่อ m_IsMounting]
  ├─ มี m_HpBone → คำนวณจาก bone WorldX/Y × PPU แล้ว TransformPoint
  └─ ไม่มี → ใช้ m_MountHpTransform.position + offset

UnMount()
  ├─ mount.OnRecycle()
  └─ ถ้า mounting → false + DoUnMount + mount.SetActive(false)
```

**รอบที่สอง:** `CreateMountActor` เจอ `m_MountActor` เดิม → reuse; `LoadBody` ใหม่;
`SeparatorSkeleton` อาจ return `true` ทันทีถ้า `separatorSlots` ยังมีของเก่า
→ ต้อง clear `separatorSlots` ถ้าต้องการ config ใหม่ (ดู `UNKNOWNS.md`)

---

## 6. Follow actor / sub-skeleton lifecycle

### 6.1 Follow actor (attach skin ที่มี cmpRootBone)

```
BuildBody (attach req)
  └─ LoadSkeletonAttachment
       └─ LoadSkeletonAttachmentByBone
            ├─ ถ้ามีใน m_FollowActors → reuse
            ├─ ไม่งั้น → CreateSkeletonActor
            │    └─ GameObject "f_{slot}_{skinID}" + UISpineActor
            ├─ fsActor.attachBoneName = ...
            ├─ fsActor.m_cmpBodySkinReq = skinReq
            └─ followActor.BuildCmpBody(fsActor, orderOffset)
                 ├─ CreateRoot/DestroySkeleton/CreateSpineComponent
                 ├─ CreateSkeletonSkinWithNoCmp
                 ├─ SperatorSlot (ถ้ามี)
                 ├─ InitWithUISpineActor → SetAnimationState(owner state)
                 └─ CreateFollow → SkeletonUtility/BoneFollowerGraphic

CancelAllReq / RecycleSelf
  └─ ReleaseFollowActor
       ├─ follow.RecycleSelf()
       └─ Destroy(follow.gameObject)
       └─ m_FollowActors.Clear()
```

### 6.2 Sub-skeleton (attach skin ที่ไม่มี cmpRootBone)

```
BuildBody (list)
  └─ CreateSubSkeleton(list)
       ├─ สร้าง SepSGRoot (ถ้ายังไม่มี)
       ├─ แยก part ของ main (SeparatorSkeletoGraphics)
       ├─ สำหรับแต่ละ req: CreateSubSkeletonGraphic → sub SG
       ├─ ผูก BoneFollowerGraphic (ถ้า followSlotName != "None")
       ├─ FlushTintBlack
       ├─ sort ตาม slot index
       └─ reparent main + part ใต้ SepSGRoot

DestroySkeleton
  ├─ คืน separator parts เข้า SG
  ├─ destroy sub ทั้งหมด
  ├─ destroy main SG
  └─ destroy SepSGRoot
```

---

## 7. Timing hazards / race conditions

| # | สถานการณ์ | พฤติกรรมจริง | ผลกับ clone |
|---|---|---|---|
| 1 | `Mount` แล้วเรียก `SeparatorSkeleton` ทันที | mount ยังโหลดไม่เสร็จ → `skeletonGraphic == null` → `false` → fallback sibling | ต้องแยก "จัด sibling ชั่วคราว" กับ "แยก part จริง" |
| 2 | `UIModelCacheItem` เรียก `StopAnimationState` หลัง `LoadBody` | `m_IAnimationState == null` → early return **ก่อน** บันทึก state → animation ที่ขอไม่ถูกจำ | ถ้าต้อง compat ต้องทดสอบภาพ cache จริง |
| 3 | `SetSlotAttachment` ก่อนโหลดเสร็จ | `m_SkeletonGraphic == null` → no-op เงียบ | ต้อง queue หรือยอมรับ no-op |
| 4 | `SetGray` ก่อนโหลด | เก็บ `m_Gray`; ตอนสร้างใช้ `gray` param; `BuildBody` เรียก `SetGray` อีกครั้ง | ผลลัพธ์ถูกต้อง |
| 5 | `SetUseClipping` ก่อนโหลด | เก็บ flag; `CreateSkeletonGraphic` ใช้ตอนสร้าง | ถูกต้อง |
| 6 | `SetUpdateMode` ก่อนโหลด | เก็บ enum; `CreateSpineComponent` ใช้ตอนสร้าง | ถูกต้อง |
| 7 | `ForceUpdate` ก่อนโหลด | SG null → no-op | ปลอดภัย |
| 8 | `PlaySlotEffect` ก่อนมี Canvas | `m_SkeletonGraphic.canvas` null → NRE | ต้อง guard |
| 9 | recycle ระหว่าง AB กำลังโหลด | `CancelAllReq` → `Cancel`; `SkinReq.OnLoaded` เช็ค `m_IsLoading` → ไม่ callback | ปลอดภัยถ้าใช้ pattern เดียวกัน |
| 10 | `LoadBody` ซ้ำขณะ SG เก่าอยู่ | SG เก่าอยู่จน `BuildBody` → `CreateSpineComponent` → `DestroySkeleton` | ภาพเก่าค้างระหว่างโหลด |
| 11 | `OnAttachmentLoaded` หลาย skin name | `ForceUpdate` ในลูป → advance หลายเฟรม | animation อาจกระโดด |
| 12 | `SeparatorSkeleton` เรียกซ้ำด้วยชื่อใหม่ | `separatorSlots.Count > 0` → return true ทันที; ไม่เปลี่ยน | ต้อง clear ก่อน |
| 13 | `SetFlipY` หลังมี `SepSGRoot` | ไม่ reset `m_SkeletonTransform.localScale` | flip อาจซ้อน |
| 14 | `SetGray` หลังมี sub หลายตัว | `m_Mat`/`m_GrayMat` เป็นของ sub ตัวท้าย | material อาจผิด |
| 15 | `DestroySkeleton` ขณะ follower cache ยังมี | cache ชี้ Transform ที่ถูก destroy | ต้อง clear ตอน destroy/recycle |

---

## 8. สรุป lifecycle สำหรับ implement

1. **Two-phase:** สร้าง actor → โหลด async → build; ห้ามถือว่า SG พร้อมทันทีหลัง `LoadBody`
2. **Idempotent destroy:** `DestroySkeleton` ต้องเรียกซ้ำได้ (field null หลัง destroy)
3. **Reuse ได้:** `RecycleSelf` เคลียร์ state แต่ไม่ destroy Root/mount; host destroy GO เอง
4. **Ownership ชัด:** material = shared; RT = host; AB ref = SkinReq; effect = effect system
5. **Virtual hooks คือจุดเชื่อม:** `CreateSpineComponent`, `DestroySkeleton`, `SetupSpine`,
   `CreateSubSkeleton`, `CreateSkeletonActor`, `BuildCmpBody`, `CreateFollow`,
   `RecycleSelf`, `Mount/UnMount/UpdateMount`, animation hooks
6. **ห้าม synchronous assumption** กับ mount/separator/cache capture
