# CALLGRAPH — `ActionSystem.UISpineActor`

เอกสารนี้แสดงเส้นทางเรียก (caller/callee) ที่ตรวจจาก metadata + decompile
และชี้จุดที่การ dispatch เป็น **virtual/interface** ซึ่งอ่าน call edge ตรง ๆ จาก metadata ไม่ได้ทั้งหมด

สถานะ: ทุกเส้นใน §1–§4 เป็น **Observed**; บางข้อความเป็น **Derived** ตามที่ระบุ

---

## 1. เส้นทางจากภายนอกถึง `UISpineActor`

```
Lua / UI prefab / CSGUIWidgetBase
        │
        ▼
UIModelShow  (MonoBehaviour, ถือ ActorSystem.IActor m_ActorTarget)
   │
   ├─ SetTarget(modelID, scale, comps, animID, stopAnim, softMask, fadeIn, action)
   │     ├─ CreateActor(modelID)
   │     │     ├─ DragonBonesConfig.IsSpineBody(modelID)?
   │     │     │     true  → new GameObject("SpineActor")
   │     │     │              + AddComponent<UISpineActor>()        ◄── จุดสร้าง #1
   │     │     │     false → new GameObject("DragonActor") + DragonBonesActor
   │     │     └─ InitSoftMask() / RemoveTarget()
   │     ├─ InitUISpinePara(canvasGroupCompatible, PMA, tintBlack)   ─┐
   │     │     └─ EnableCanvasGroupCompatible / EnablePmaVertexColor  │ เฉพาะ UI
   │     │        / EnableTintBlack                                  │
   │     ├─ FlushHUDTarget() → GetHudOffset/GetFlipX                 │
   │     ├─ m_ActorTarget.SetAnimationState(animID)  ──── interface ─┤
   │     ├─ m_ActorTarget.GUILoadBody(...)           ──── interface ─┤
   │     │     callback → ถ้า stopAnim: StopAnimationState(animID)    │
   │     │              → OnLoadBodyFinish → FlushHUDTarget           │
   │     │                                 → InitFollow (dragon only) │
   │     │                                 → GenRenderTexure (RT)     │
   │     └─ m_ActorTarget.SetSortingLayer("Default") ─── interface ──┘
   │
   └─ อื่น ๆ: SetAnimationState(WithProgress) / StopAnimationState / SetGray /
      SetColor / FadeToColor / SetAnimationSpeed / Mount / UnMount /
      SetTargetFlipX/Y / ChangeSlotDisplay / AddSkeletonSlot /
      LoadSlot / ChangeFollowActorSlotDisplay / PlayEffectOnActor
              │
              ▼  (interface dispatch — ส่วนใหญ่ไม่ปรากฏเป็น direct call edge ไป UISpineActor)
       ActorSystem.IActor  ──implemented by──  SpineActor
                                                   ▲
                                                   │ extends
                                              UISpineActor
```

### 1.1 จุดสร้าง / จุดอ้างถึงตรง (ทั้งโปรเจกต์)

| จุด | ไฟล์:บรรทัด | สิ่งที่ทำ |
|---|---|---|
| `UIModelShow.CreateActor` | `UIModelShow.cs:219-223` | `new GameObject("SpineActor")` + `AddComponent<UISpineActor>()`; `m_ActorTarget = m_uiSpineActor` |
| `UIModelCacheItem` | `UIModelCacheItem.cs:139-144` | `new GameObject("CacheSpineActor")` + `AddComponent<UISpineActor>()` |
| `UIModelShow.SetSlotAttachment` | `UIModelShow.cs:383-392` | cast `m_ActorTarget as UISpineActor` (ถ้าไม่ใช่ → no-op) |
| `UIModelShow.EnableUpdateRendererSize` | `UIModelShow.cs:278-284` | เรียกตรงบน `m_uiSpineActor` |
| `UIModelShow.InitUISpinePara` | `UIModelShow.cs:286-297` | เรียกตรง 3 setters |
| `UIModelShow.SetMountSeparaterSlotName` | `UIModelShow.cs:464-470` | เรียกตรง `SetMountSeparatorSlotNames` (public ของ base) |
| `UIModelCacheItem.SetTarget` | `UIModelCacheItem.cs:154-157` | เรียกตรง `SetUseClipping(true)` |
| `UIModelCacheItem.CaptureImageOnNextFrame` | `UIModelCacheItem.cs:192-195` | เรียกตรง `ForceUpdate()` |

> **Derived:** ไม่มี direct call จากภายนอกเข้าเมธอดอื่นของ `UISpineActor` เลย
> เพราะ host ถือ `IActor`; การเรียกส่วนใหญ่จึงเป็น interface dispatch
> (metadata call edge จะชี้ไป `IActor`/`SpineActor` ไม่ใช่ `UISpineActor`)

### 1.2 `UIModelShow` → actor (เฉพาะ UI)

| เมธอด host | ปลายทาง | หมายเหตุ |
|---|---|---|
| `SetTarget` | `InitUISpinePara` → 3 setters | ก่อน `GUILoadBody`; มีผลตอนสร้าง SG |
| `SetTarget` | `SetAnimationState(animID)` | ก่อนโหลด (ถ้า `!stopAnim`) |
| `SetTarget` callback | `StopAnimationState(animID)` | หลังโหลด (ถ้า `stopAnim`) |
| `EnableUpdateRendererSize` | `EnableUpdateRendererSize` | ตรง |
| `SetSlotAttachment` | `SetSlotAttachment` | cast เป็น `UISpineActor` เท่านั้น |
| `SetMountSeparaterSlotName` | `SetMountSeparatorSlotNames` (base) → mount actor | ตรง |
| `SetSlotDisplayIndex` | `DragonBonesActor` เท่านั้น | spine: **no-op** |
| `InitFollow` | `FollowPair.Init(IActor)` | `FollowPair` รองรับเฉพาะ `DragonBonesActor` → spine: **no-op** |
| `RemoveTarget` | `IActor.OnRecycle()` → `SpineActor.RecycleSelf()` | virtual → UI `RecycleSelf` |
| `Update` | `GenRenderTexure()` ถ้ามี RawImage | → `IActor.DrawToBuffer` → UI **no-op** |
| `OnDestroy` | `ReleaseSoftMask` + `RemoveTarget` | |

### 1.3 `UIModelCacheItem` → actor

```
UIModelCacheItem.SetTarget(...)
  ├─ (กรณี spine) new GameObject("CacheSpineActor") + UISpineActor
  ├─ m_ActorTarget.LoadBody(modelID, comps, isGUI:true, scale, false, onLoadFinish)
  │     └─ SpineActor.LoadBody → CreateRoot → CancelAllReq → StartLoadMain
  ├─ m_ActorTarget.SetUpdateMode(3)          // FullUpdate
  ├─ m_ActorTarget.StopAnimationState(animID, 1f)
  ├─ m_ActorTarget.SetSortingOrder(...)
  ├─ m_ActorTarget.SetSortingLayer("Default")
  ├─ m_ActorTarget.SetOffset(...)
  └─ uISpineActor.SetUseClipping(true)
       (ทำหลัง LoadBody — มีผลกับ SG ที่สร้างภายหลัง)
```

หลัง capture: `ForceUpdate()` → `OnRecycle()`

---

## 2. เส้นทางโหลด (asynchronous)

```
LoadBody / GUILoadBody
  │
  ├─ set m_Scale, m_IsSmall, m_OnLoadFinish, m_FadeInTime (GUI)
  ├─ CreateRoot()                       [ถ้ามีอยู่แล้วไม่สร้างใหม่]
  ├─ CancelAllReq()
  │    ├─ ReleaseSkinReq()              (คืน AB refcount / cancel)
  │    ├─ CanelAllImageDisplayReq()
  │    ├─ CancelRelaceAttachment()
  │    └─ ReleaseFollowActor()          (RecycleSelf + Destroy GO)
  └─ StartLoadMain(bodyID, slots)
       ├─ DragonBonesConfig.GetDBBodyConfig
       ├─ set width/height/root scale
       ├─ FlushHudOffset()              ◄── virtual → UI: ×125
       ├─ new SkinReq(main...) ; Start()  ◄── async AB
       └─ add attach SkinReq ตาม cmps (ถ้า !noCmp)

        ┌──────────── AB โหลดเสร็จ ────────────┐
        ▼                                       ▼
 OnBodyLoadFinish                        OnAttachSkinFinish (นับจนครบ)
   ├─ StartLoadAttachSkins()                 │
   │    ├─ ไม่มี attach → BuildBody()        │
   │    └─ มี → start ทุก req ───────────────┘
   ▼
 BuildBody()                                  [protected, ไม่ virtual]
   ├─ CreateSpineComponent(data)              ◄── virtual → UI สร้าง SkeletonGraphic
   ├─ ถ้า overrideSpine → SkeletonUtility.SpawnHierarchy
   ├─ ไม่มี attach: CreateSkeletonSkinWithNoCmp
   ├─ มี attach:
   │    ├─ BuildSlotMap()
   │    ├─ รวม skin
   │    ├─ ผูก AnimationState.Event
   │    ├─ LoadSkeletonAttachment / LoadSkeletonAttachmentByBone (ต่อ slot)
   │    │     ├─ CreateSkeletonActor(...)     ◄── virtual → UI สร้าง UISpineActor ลูก
   │    │     └─ BuildCmpBody(...)            ◄── virtual → UI สร้าง SG + follow
   │    └─ CreateSubSkeleton(list)            ◄── virtual → UI สร้าง sub-SG
   ├─ fade-in setup
   ├─ m_SkeletonTransform.localScale = flip
   ├─ SetAnimationState(m_StateID, m_AnimationSpeed)
   │     └─ UpdateAnimationState(...)         ◄── virtual → UI sync sub
   ├─ SetFreezeAnimation(m_FreezeAnimation)   ◄── virtual; UI: no-op
   ├─ DoUpdateColor()                         ◄── virtual → UI (.color)
   ├─ FlushShadow()                           (mesh only)
   ├─ ReplaySlotEffect() → PlaySlotEffect     ◄── virtual → UI PlayUIEffectEx
   ├─ StartLoadImageDisplay()
   ├─ StartLoadAttachment()
   ├─ SetGray(m_Gray)                         ◄── virtual → UI สลับ material
   ├─ m_OnLoadFinish?.Invoke()
   └─ SetupSpine()                            ◄── virtual → UI rect size/flip
```

> `CreateSpineComponent` ถูกเรียกทั้งจาก `BuildBody` (ตัวหลัก) และ `BuildCmpBody` (follow actor)
> ตัวหลังเรียก `DestroySkeleton` ก่อนแล้ว และ `CreateSpineComponent` เรียก `DestroySkeleton` ซ้ำ
> (idempotent เพราะ field ถูก set null)

---

## 3. Virtual dispatch matrix

base `SpineActor` ประกาศ virtual ไว้ให้ subclass แทนที่; UI override ทั้งหมดนี้

| base virtual | UI override | พฤติกรรม UI |
|---|---|---|
| `CreateSpineComponent(SkeletonDataAsset)` | ✔ | สร้าง `SkeletonGraphic` แทน `SkeletonAnimation` |
| `DestroySkeleton()` | ✔ | คืน parts + destroy sub/SG/SepSGRoot |
| `SetupSpine()` | ✔ | rect size + allowMultiple + hide slots + flip |
| `OnAttachmentLoaded(ReplaceAttachmentReq)` | ✔ | slot map + copy offset + ForceUpdate |
| `StopAnimationState(int,float)` | ✔ | main stop + sync sub (ไม่บันทึก state) |
| `SetUpdateMode(int)` | ✔ | main + sub `updateWhenInvisible` |
| `FlushHudOffset()` | ✔ | ×125 |
| `DoUpdateColor()` | ✔ | alpha fade-in เท่านั้น (ไม่ premultiply RGB) |
| `UpdateColor_Imp(Color)` | ✔ | `SG.color` + sub |
| `SetGray(bool)` | ✔ | สลับ material |
| `GetBoneFollowerTransform(string)` | ✔ | `BoneFollowerGraphic` |
| `PlaySlotEffect(SlotEffectInfo)` | ✔ | `PlayUIEffectEx` |
| `DrawToBuffer(CommandBuffer)` | ✔ | ว่าง |
| `SetShaderType(int)` | ✔ | ว่าง |
| `RecycleSelf()` | ✔ | base + reset 5 flag |
| `CreateSkeletonActor(...)` | ✔ | follow actor เป็น `UISpineActor` |
| `BuildCmpBody(FollowSpineActor,int)` | ✔ | สร้าง SG + animation + follow |
| `CreateFollow(FollowSpineActor)` | ✔ | `SkeletonUtility` + `BoneFollowerGraphic` |
| `FlushSlashProperty()` | ✔ | LogError |
| `CreateMountActor()` | ✔ | mount เป็น `UISpineActor` |
| `SeparatorSkeleton(string[],int,int)` | ✔ | separator parts แบบ UI |
| `Mount(...)` | ✔ | base + separator/fallback sibling |
| `UnMount()` | ✔ | base เท่านั้น |
| `UpdateMount()` | ✔ | bone × `referencePixelsPerUnit` |
| `CreateSubSkeleton(List<SkinReq>)` | ✔ | sub-SG + จัดลำดับ part |
| `SetFlipX/SetFlipY(bool)` | ✔ | ผ่าน `SepSGRoot` ถ้ามี |
| `UpdateAnimationState(...)` | ✔ | sub เท่านั้น; ว่างถ้าไม่มี sub |
| `UpdateAnimationStateWithProgress(...)` | ✔ | sub + progress |
| `UpdateAnimationSpeed(float)` | ✔ | sub `timeScale=1` (ทิ้งค่า) |

virtual ที่ **ไม่** override (ใช้ base): `skeletonGraphic` property (override),
`SetFreezeAnimation`, `SetColor`, `FadeToColor`, `SetAnimationSpeed` (base),
`SetAnimationState`, `SetAnimationStateWithProgress`, `ChangeSlotDisplay`,
`LoadSlot`, `HideSlot`, `AddSkeletonSlot`, `ChangeFollowActorSlotDisplay`,
`PlayEffectOnActor`, `RayHit`, `RayHitPerspective`, `SetCullableManager`,
`SetSortingLayer/Order`, `SetScale`, `SetOffset`, `Twinkle`, `ShowShadow`, ฯลฯ

> **สำคัญ:** `SpineActor.SetFreezeAnimation` เป็น `virtual` แต่ UI ไม่ override
> → base เก็บ flag แล้วพยายาม set `m_SkeletonAnimation.FreezeAnimation` ซึ่งเป็น null
> → **ไม่มีผลกับ UI** (Observed จาก field `m_SkeletonAnimation` ที่ UI ไม่เคย set)

---

## 4. Internal graph ของ `UISpineActor` (เฉพาะเส้นที่สำคัญ)

### 4.1 Creation / teardown

```
CreateSpineComponent ──► DestroySkeleton
        │                     └─► (destroy sub-SG, SG, SepSGRoot; clear slot map)
        └─► CreateSkeletonGraphic ──► SkeletonGraphic.NewSkeletonGraphicGameObject
                                        └─► AddSkeletonGraphicComponent → Initialize

BuildCmpBody ──► CreateRoot
        ├─► DestroySkeleton
        ├─► CreateSpineComponent ──► CreateSkeletonGraphic
        ├─► CreateSkeletonSkinWithNoCmp (base)
        ├─► SperatorSlot ──► SetSerparatorSlotNames + LateUpdate + reparent parts[0]
        ├─► InitWithUISpineActor ──► SetAnimationState
        └─► CreateFollow ──► SkeletonUtility.SpawnBone × N
                          └─► BoneFollowerGraphic

CreateSubSkeleton
        ├─► SeparatorSkeletoGraphics (main)
        ├─► CreateSubSkeletonGraphic ──► CreateSkeletonGraphic
        │        ├─► FindSkin/AddSkin/SetSkin
        │        └─► SeparatorSkeletoGraphics (sub)
        │                 └─► SetSerparatorSlotNames + LateUpdate
        ├─► BoneFollowerGraphic (ถ้า followSlotName != "None")
        ├─► FlushTintBlack
        └─► reparent main + parts ใต้ SepSGRoot + sort by slot index
```

### 4.2 Runtime update

```
Unity Update (SpineActor.Update)  [เฉพาะเมื่อ m_ISkeleton != null]
  ├─► UpdateMount()            ◄── virtual → UI (bone × PPU)
  ├─► m_Slash.Update → FlushSlashProperty()  ◄── virtual → UI LogError
  └─► UpdateColor(dt)
        └─► DoUpdateColor()    ◄── virtual → UI (.color + follow actors)

SkeletonGraphic.Update() (Unity player loop ของ SG เอง)
  └─ อัปเดต animation/mesh ตาม updateMode
```

### 4.3 Slot / attachment / effect

```
UIModelShow.SetSlotAttachment
  └─► UISpineActor.SetSlotAttachment
        └─► SkeletonGraphic.Skeleton.SetAttachment(slot, attachment)

IActor.ChangeSlotDisplay (base)
  ├─ id == 0 → SetAttachment ตรง
  └─ id > 0  → ReplaceAttachmentReq.Start()
                  └─ OnLoaded → OnAttachmentLoaded (UI)
                        ├─ BuildSlotMap
                        ├─ ถ้า isSkeleton → LoadSkeletonAttachment → CreateSkeletonActor/BuildCmpBody
                        ├─ else → copy offset (ถ้า useSrcOffset) + set attachment
                        └─ ForceUpdate (ถ้า SkeletonData เปลี่ยน)

IActor.PlayEffectOnActor (base)
  ├─ เก็บ SlotEffectInfo
  └─ PlaySlotEffect (UI) ──► GetBoneFollowerTransform
                              └─► BoneFollowerGraphic
                        └─► EffectSystemLocator.effectManager.PlayUIEffectEx
```

### 4.4 Reverse map: ใครเรียกเมธอด public/protected ของ UI

| ปลายทาง | ผู้เรียก (observed) |
|---|---|
| `EnableUpdateRendererSize` | `UIModelShow` |
| `EnablePmaVertexColor` / `EnableCanvasGroupCompatible` / `EnableTintBlack` | `UIModelShow.InitUISpinePara` |
| `SetSlotAttachment` | `UIModelShow.SetSlotAttachment` |
| `ForceUpdate` | `OnAttachmentLoaded`, `UIModelCacheItem` |
| `SetUseClipping` | `UIModelCacheItem` |
| `StopAnimationState` | `UIModelShow` (interface), `UIModelCacheItem` (interface) |
| `SetUpdateMode` | `UIModelCacheItem`, base `OnMountLoadFinish` (mount actor) |
| `SeparatorSkeleton` | ตัวเอง (`Mount`), base `DoMount`, base `SetSeparatorSlotName` (virtual) |
| `Mount` / `UnMount` | `UIModelShow` (interface) |
| `SetFlipX/Y` | `UIModelShow.SetTargetFlipX/Y` (interface), `SetupSpine`, base `OnMountLoadFinish` |
| `SetGray` | `UIModelShow.SetGray`, base `BuildBody` |
| `SetUpdateMode` | `UIModelCacheItem`, base mount |
| `DrawToBuffer` | `UIModelShow.GenRenderTexure` / `CSGUIDragoneBoneImage` (interface) |
| `CreateSubSkeleton` | base `BuildBody` |
| `OnAttachmentLoaded` | `ReplaceAttachmentReq` callback |
| `RecycleSelf` | base `OnRecycle` (interface) |

---

## 5. Follow actor graph (attach skin ที่มี `cmpRootBone`)

```
Owner (UISpineActor)
  └─ m_FollowActors: Dictionary<string slot, FollowSpineActor>
        └─ FollowSpineActor
             ├─ ownerActor        : SpineActor (เจ้าของ)
             ├─ followActor       : UISpineActor  (GameObject "f_{slot}_{skinID}")
             ├─ attachBoneName    : bone เป้าหมายบน owner
             ├─ cmpRootBoneName   : bone ฐานของ follow skeleton
             ├─ separatorSlotName : ชื่อ slot ที่ต้องแยก
             ├─ listenAnimEvent   : จาก dbcomponentcfg
             └─ m_cmpBodySkinReq  : SkinReq ของ component

CreateSkeletonActor (UI)
  └─ GameObject "f_{slot}_{skinID}"
        parent = owner.transform          ◄── ต่างจาก base (parent = owner.Root)
        └─ UISpineActor (follow)
              ├─ BuildCmpBody(owner)
              │    ├─ CreateRoot → DestroySkeleton → CreateSpineComponent
              │    ├─ CreateSkeletonSkinWithNoCmp
              │    ├─ SperatorSlot(owner.ModelRoot, separatorSlotName) [ถ้ามี]
              │    ├─ InitWithUISpineActor → SetAnimationState(owner.StateID, owner.AnimationSpeed)
              │    └─ CreateFollow
              │         ├─ SkeletonUtility (บน SG หลักของ follow)
              │         │    boneRoot = follow.Root
              │         ├─ SpawnBone(root ancestor → … → target)  [Mode.Override]
              │         └─ BoneFollowerGraphic
              │              skeletonGraphic = owner.m_SkeletonGraphic
              │              bone           = attachBoneName
              └─ animation forwarding:
                   base SetAnimationState
                     └─ SetFollowActorAnimationState(...)
                          └─ ถ้า listenAnimEvent == false → follow.SetAnimationState(...)
                   AnimationState.Event → HandleFollowActorAnimationState
                          └─ ถ้า listenAnimEvent == true → follow.SetAnimationState(event.Int)
```

**ข้อควรระวัง:**
- `CreateSkeletonActor` UI อ่าน `GetDBComponentcfg(skinID).listenAnimEvent` โดยไม่กัน null
- `CreateFollow` cast `fsActor.ownerActor` เป็น `UISpineActor` ตรง ๆ
- `ReleaseFollowActor` (base) ทำ `followActor.RecycleSelf()` + `Destroy(gameObject)`
  → follow actor ต้องรองรับ recycle ซ้ำ

---

## 6. Mount graph และ timing

```
Owner.Mount(id, slots, hp, scale, offset, cb)          [UI override]
  ├─ base.Mount(...)
  │    ├─ CreateMountActor()                            [UI override]
  │    │    ├─ (ถ้าไม่มี) GameObject "SpineMount" + UISpineActor
  │    │    ├─ CreateRoot() → SortingGroup
  │    │    └─ copy flip + sortingLayer/Order + ComputerSortIndex
  │    ├─ m_MountHP = hp ; m_MountOffset = offset ; m_OnMountFinish = cb
  │    └─ m_MountActor.LoadBody(id, slots, isGUI:false, scale, false, OnMountLoadFinish)
  │         └─ ... async ... ──► OnMountLoadFinish
  │                                 ├─ ถ้า mounting อยู่ → DoUnMount
  │                                 ├─ m_IsMounting = true
  │                                 ├─ mount.SetActive(true)
  │                                 ├─ mount.SetFlipX/Y(owner)
  │                                 ├─ mount.SetUpdateMode(owner.m_InvisbleUpdateMode)
  │                                 ├─ DoMount()
  │                                 │    ├─ m_MountHP ว่าง → m_MountHpTransform = mount.Root
  │                                 │    ├─ ไม่งั้น → m_HpBone = mount.Skeleton.FindBone(hp)
  │                                 │    │           m_MountHpTransform = mount.Root
  │                                 │    └─ mount.SeparatorSkeleton(owner.m_SeparatorSlotNames, ...)
  │                                 └─ cb?.Invoke()
  │
  └─ m_MountActor.SeparatorSkeleton(...)     ◄── เกิดก่อน async เสร็จ
       ├─ false → offset.z > 0 ? mount.SetAsLastSibling() : SetAsFirstSibling()
       └─ true  → (กรณี reuse ที่โหลดเสร็จแล้ว)

Owner.Update (ทุกเฟรม)
  └─ UpdateMount() [UI]
       ├─ m_HpBone != null:
       │    canvas = SG.canvas ?? SG.GetComponentInParent<Canvas>()
       │    ppu = canvas ? canvas.referencePixelsPerUnit : 100
       │    p = mount.SG.transform.TransformPoint(bone.WorldX*ppu, bone.WorldY*ppu, 0)
       │    p.z = mount.Root.position.z
       │    owner.Root.position = p + m_MountOffset
       └─ ไม่งั้น: owner.Root.position = m_MountHpTransform.position + m_MountOffset

Owner.UnMount()
  └─ base.UnMount()
       ├─ mount.OnRecycle()   (RecycleSelf → DestroySkeleton ฯลฯ)
       └─ ถ้า mounting → m_IsMounting=false; DoUnMount(); mount.SetActive(false)
```

**สรุป timing (Derived):**
1. `Mount` คืนก่อน mount โหลดเสร็จ
2. การ `SeparatorSkeleton` ครั้งแรกจึงมัก fail → fallback sibling
3. เมื่อโหลดเสร็จ base `DoMount` เรียก `SeparatorSkeleton` ซ้ำ (ตอนนี้ SG พร้อม) → สำเร็จ
4. `UpdateMount` เริ่มทำงานเมื่อ `m_IsMounting == true` (หลังโหลดเสร็จ)
5. `UnMount` ไม่ destroy mount; รอบถัดไป reuse actor เดิม (และ `SeparatorSkeleton`
   อาจ return true ทันทีเพราะ `separatorSlots` ยังมีอยู่)

---

## 7. Separator reparent graph — สองเส้นทางที่ต่างกัน

### 7.1 `SeparatorSkeleton` (mount / `SetSeparatorSlotName`)

```
owner.transform
  ├─ ... sibling อื่น ...
  ├─ SpineActor (owner)
  │    └─ Root
  │         └─ New Spine GameObject (SkeletonGraphic)
  │              └─ (SeparatorParts เดิม)
  └─ SpineMount (mount actor)

หลัง SeparatorSkeleton สำเร็จ:
owner.transform.parent
  ├─ [part 0]  sibling index = baseOrder >= 0 ? 2 : 0
  ├─ [part 1]  sibling index = ... + increment
  └─ ... (ย้ายออกจาก SG ไปเป็น sibling ของ actor)
```

- reparent ไป **`base.transform.parent`** (parent ของ actor)
- `SetParent(parent)` ไม่ระบุ `worldPositionStays` → **true** (รักษาตำแหน่งโลก)
- sibling index เริ่ม `j+2` เมื่อ `baseOrder >= 0`; ไม่งั้นเริ่ม `j`
- `baseOrder` เพิ่มทีละ `sortingOrderIncreament` ต่อ part

### 7.2 `SperatorSlot` (follow actor ใน `BuildCmpBody`)

```
followActor.BuildCmpBody
  └─ SperatorSlot(root = owner.ModelRoot, name)
       ├─ SG.allowMultipleCanvasRenderers = true
       ├─ SG.enableSeparatorSlots = true
       ├─ SG.SetSerparatorSlotNames([name])
       ├─ SG.LateUpdate()
       └─ parts[0].SetParent(owner.ModelRoot, worldPositionStays:false)
          parts[0].SetAsFirstSibling()
```

- reparent ไป **`owner.ModelRoot`** (ไม่ใช่ parent ของ actor)
- `worldPositionStays:false`
- ใช้เฉพาะ part แรก

### 7.3 `CreateSubSkeleton` (attach skin ที่ไม่มี cmpRootBone)

```
actor
 └─ Root
      └─ SepSGRoot                 ◄── สร้างถ้ายังไม่มี
           ├─ [main SG / parts เรียงตาม slot index]
           ├─ [sub SG 1 / parts]
           └─ [sub SG 2 / parts]
```

- main SG ถูก `SetParent(SepSGRoot, false)` หลังสร้าง sub ทั้งหมด
- part/sub ถูก sort ด้วย `IndexTransform.index` (slot index ของ main skeleton)
- `SetSiblingIndex(num)` ตามลำดับที่ sort แล้ว
- ผลคือ draw order ของ UI ตรงกับ slot order ของ skeleton หลัก

---

## 8. จุดที่ call edge ใน metadata ไม่ครบ (ต้องอ่านโค้ด)

1. **Interface dispatch** — `IActor` 60 เมธอด; host เรียกผ่าน interface
   ทำให้ call edge ไปลงที่ `IActor`/base ไม่ชี้ `UISpineActor`
2. **Virtual dispatch** — base เรียก hook (`CreateSpineComponent`, `BuildBody` ภายใน,
   `RecycleSelf`, `OnAttachmentLoaded`, ฯลฯ) → ปลายทางจริงคือ override ของ UI
3. **Unity message** — `Awake`, `Update`, `OnEnable`, `OnDisable`, `OnDestroy`
   ไม่มี call edge จาก managed code
4. **Delegate/callback** — `SkinReq.OnLoadFinish`, `ReplaceAttachmentReq.onLoadFinish`,
   `Action onMountFinish`, `TrackEntry.Complete`, `AnimationState.Event`
5. **Coroutine** — `UIModelCacheItem.CaptureImageOnNextFrame` (yield)
6. **Inspector/serialized** — `m_uiSpineActor`, `FollowPairs`, `modelId` ฯลฯ

> เวลาออกแบบ clone: ยึด **ลำดับจาก §2 + virtual matrix §3** ไม่ใช่ไล่ call edge ดิบ

---

## 9. ลำดับเรียกโดยย่อ ( happy path )

```
AddComponent<UISpineActor>
  └─ Awake (ModelRoot = transform)

SetTarget
  ├─ CreateActor → AddComponent
  ├─ InitUISpinePara → 3 setters
  ├─ SetAnimationState (ก่อนโหลด; m_IAnimationState == null → เก็บ state เท่านั้น)
  ├─ GUILoadBody → LoadBody → CreateRoot → CancelAllReq → StartLoadMain
  │     └─ SkinReq.Start (async)
  ├─ SetSortingLayer("Default")
  └─ [AB เสร็จ] BuildBody
        ├─ CreateSpineComponent → CreateSkeletonGraphic
        ├─ skin / sub-skeleton / follow
        ├─ SetAnimationState (ตอนนี้มี SG แล้ว)
        ├─ SetFreezeAnimation (no-op)
        ├─ DoUpdateColor
        ├─ ReplaySlotEffect
        ├─ SetGray
        ├─ onLoadFinish → OnLoadBodyFinish (host)
        └─ SetupSpine

Update (ทุกเฟรม)
  ├─ UpdateMount
  ├─ FlushSlashProperty (ถ้า lerp จบ)
  └─ DoUpdateColor (ถ้าสีเปลี่ยน)

OnRecycle
  └─ RecycleSelf
       ├─ base.RecycleSelf
       │    ├─ UnMount → mount.OnRecycle + deactivate
       │    ├─ CleanTweener
       │    ├─ CancelAllReq → ReleaseSkinReq / ReleaseFollowActor
       │    ├─ reset state
       │    ├─ DestroySkeleton (UI)
       │    └─ StopSlotEffect / clear caches / culling
       └─ reset 5 flag ของ UI
```
