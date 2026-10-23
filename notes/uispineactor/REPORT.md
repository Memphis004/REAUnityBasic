# ขั้นที่ 1 — RE Report: `UISpineActor` system

สถานะ: **Observed** = อ่านจาก metadata/CIL หรือ decompile โดยตรง, **Derived** = สรุปจากการรวม Observed หลายจุด,
**Inferred** = ตีความจากชื่อ/บริบท, **Unknown** = ยังไม่มีหลักฐาน ห้ามเติมเอง

---

## 0. หลักฐานที่ใช้ (Evidence)

| ID | ที่มา | ค่า |
|---|---|---|
| **E1** | `inspect_managed_members` (selection summary, 3 types) | `ev_e6bfb4cd52a6be3d0f436501c62fea25129fa86004d9a20ed7712ca5e89331ec` |
| **E2** | `inspect_managed_members` (ทั้ง assembly, ใช้ extract subset) | `ev_7d8b0cb6996072c714cd3523c83b3562e2b8fe1fe6bc389bafda007a21ef3b65` |
| artifact | `Assembly-CSharp.dll` | SHA-256 `62761be23a4a40518eeb0e98a2d6be6e4b11c3b1bad2064e4c8b528d9876184f`, 5,151,744 B, PE x86 |
| MVID | module | `804d1d3b-ea79-4872-9784-771b88e440f4`, metadata complete (v4.0.30319) |
| token scope | build-local, ผูกกับ SHA-256 ข้างบน | TypeDef 3883 / MethodDef 30652 / MemberRef 10455 |

การเลือก 3 type ตรงทั้งหมด (`selection.unmatched_type_names = []`) ได้ 3 types / 110 fields / 247 methods /
1,579 call edges / 1,502 field accesses / 39 user strings — coverage `complete`

ไฟล์ที่สร้างจาก E1/E2 (ไม่ใช่การเดา):

- `notes/uispineactor/members_subset.json` — rows ของ type ที่เลือก + ทุก call edge ที่แตะ type เหล่านั้น
- `notes/uispineactor/ActionSystem_UISpineActor_methods.csv` — **48 เมธอด** (token, signature, IL size, IL hash, calls)
- `notes/uispineactor/ActionSystem_SpineActor_methods.csv` — 159 เมธอด
- `notes/uispineactor/UIModelShow_methods.csv` — 40 เมธอด
- `notes/uispineactor/extract_members.py`, `analyze_callers.py` — สคริปต์ที่ใช้กรอง (รันซ้ำได้)

Type identity (Observed, E1):

| Type | token | extends |
|---|---|---|
| `ActionSystem.SpineActor` | `0x02000640` | `UnityEngine.MonoBehaviour` |
| `ActionSystem.UISpineActor` | `0x02000643` | `ActionSystem.SpineActor` |
| `UIModelShow` | `0x02000059` | `UnityEngine.MonoBehaviour` |
| `ActionSystem.FollowSpineActor` | `0x02000642` | `System.Object` (data holder 7 fields) |
| `ActionSystem.ReplaceAttachmentReq` | `0x02000641` | `System.Object` |

> Type 8020 อยู่ใน `Assembly-CSharp.dll` — **ไม่ต้อง** ใช้ `Assembly-CSharp-firstpass.dll`
> (ตรวจแล้ว: type เหล่านี้ไม่ปรากฏใน firstpass)

---

## 1. ภาพรวม: ใครเป็นอะไร (Observed + Derived)

```
UIModelShow (UI prefab component, MonoBehaviour)
  └─ CreateActor(modelID)
       ├─ DragonBonesConfig.IsSpineBody(modelID) == true
       │    → new GameObject("SpineActor") + AddComponent<UISpineActor>()   [UIModelShow.cs:219-223]
       └─ false → DragonBonesActor
  └─ m_ActorTarget : ActorSystem.IActor        (interface call ทุกครั้ง)
       ├─ GUILoadBody(...)  → SpineActor.LoadBody → StartLoadMain → SkinReq → BuildBody
       └─ SetAnimationState / StopAnimationState / SetGray / Mount / UnMount / ...

UIModelCacheItem (cache/prewarm)                       [UIModelCacheItem.cs:139-155]
  └─ new GameObject("CacheSpineActor") + AddComponent<UISpineActor>()

Entity / BaseEntity(airMiniGame) / CSGUIDragoneBoneImage
  └─ AddComponent<SpineActor>()   → world/3D path (SkeletonAnimation + MeshRenderer)
```

**Derived (สำคัญ):** `IActor` เป็น contract เดียวที่ UI ใช้ (`UIModelShow.m_ActorTarget` เป็น `IActor`)
จึงไม่มี direct call edge จากภายนอกเข้า `UISpineActor` ยกเว้นตัวสร้าง 2 จุดข้างบน — call edge ที่เห็นใน metadata
จึงเป็นภายในระบบ (`UIModelShow.SetSlotAttachment → UISpineActor.SetSlotAttachment` ฯลฯ)
และ method ส่วนใหญ่ถูกเรียกผ่าน interface dispatch (spine ของ clone ไม่ต้องเลียนแบบ interface 60 เมธอด)

**ความรับผิดชอบจริง (Observed):**

| คลาส | rendering | lifecycle | เด่นเฉพาะ |
|---|---|---|---|
| `SpineActor` | `SkeletonAnimation` + `MeshRenderer` + `SortingGroup`, `MaterialPropertyBlock` (color/flash), `CommandBuffer.DrawMesh` | `LoadBody`→`StartLoadMain`→`SkinReq` (async AB) →`BuildBody`; `RecycleSelf` คืนทุกอย่าง | world actor, mount แบบ mesh, `SkeletonRenderSeparator`, shadow, light, raycast 2D/3D |
| `UISpineActor` | `SkeletonGraphic` (CanvasRenderer) ไม่มี MeshRenderer เลย | override `CreateSpineComponent`, `DestroySkeleton`, `SetupSpine`, `RecycleSelf` | material swap (gray/PMA/tintBlack), separator *parts* ที่เป็น UI transform, rect size, sub-skeleton เป็น SkeletonGraphic ซ้อน |
| `UIModelShow` | ไม่ render เอง; ถือ `IActor` + HUD transform | `CreateActor`/`RemoveTarget` (destroy GO) | เลือก spine/dragon, scale/anim, HUD offset, follow pairs, RawImage + CommandBuffer (RT) |

---

## 2. `ActionSystem.UISpineActor` — 48 เมธอด

### 2.1 เมธอดที่มีพฤติกรรมจริง (ตารางหลัก)

| เมธอด (token) | ถูกเรียกจาก | ทำอะไรจริง | หลักฐาน |
|---|---|---|---|
| `CreateSkeletonGraphic(SkeletonDataAsset, bool gray, bool tintBlack=false)` `0x0600498d` (IL 434) | `CreateSpineComponent`, `CreateSubSkeletonGraphic` | เลือก material 4 ชุดตาม `m_TintBlack` × `m_CanvasGroupCompatible` จาก `GUIConfig`; `SkeletonGraphic.NewSkeletonGraphicGameObject(data, m_RootTransform, mat)`; ถ้า tintBlack ตั้ง `MeshGenerator.settings.tintBlack`, `canvasGroupTintBlack`, และ `canvas.additionalShaderChannels \|= TexCoord1\|TexCoord2`; ตั้ง `pmaVertexColors`; `allowMultipleCanvasRenderers = s_UISpineAlwaysMult \|\| atlasAssets.Length>1 \|\| (len==1 && MaterialCount>1)`; `settings.useClipping`; `raycastTarget` | UISpineActor.cs:119-166 |
| `CreateSpineComponent` `0x0600498e` | `BuildBody` (base virtual) | `DestroySkeleton()` → `CreateSkeletonGraphic(...)` → `updateWhenInvisible` → ผูก `m_ISkeleton`/`m_IAnimationState`/`m_SkeletonTransform` → **`m_SortingGroup.enabled=false`** | :168-179 |
| `DestroySkeleton` `0x0600498c` (IL 338) | `CreateSpineComponent`, `RecycleSelf` (ผ่าน base) | คืน separator parts ไปเป็นลูกของ `SkeletonGraphic.transform` ก่อน, destroy sub-skeleton ทุกตัว + `Clear()`, destroy GameObject ของ skeleton, set field เป็น null, destroy `SepSGRoot`, `m_SlotMap.Clear()` | :78-117 |
| `SetupSpine` `0x06004990` | `BuildBody` ท้ายสุด (virtual hook) | `UpdateRectTranformSize()`; ถ้ามี attach skin → `allowMultipleCanvasRenderers=true`; `DoHideSlots()`; `SetFlipX(m_FlipX)` | :235-244 |
| `SetGray(bool)` `0x0600499c` | `IActor.SetGray` (`UIModelShow.SetGray`) | **สลับ material เท่านั้น**: `m_SkeletonGraphic.material = gray ? m_GrayMat : m_Mat` + sub-skeletons ทุกตัว (material ถูกเลือกไว้ตั้งแต่ `CreateSkeletonGraphic`) | :382-399 |
| `SetSlotAttachment(slot,attachment)` `0x06004997` (IL 33) | `UIModelShow.SetSlotAttachment` | `m_SkeletonGraphic.Skeleton.SetAttachment(slotName, attachName)` ตรง ๆ ไม่โหลด asset, ไม่แก้ `m_SlotMap`, ไม่มี fallback เมื่อ slot ไม่มี | :323-329 |
| `UpdateAnimationState(name,loop,speed)` `0x060049b3` | base `SetAnimationState` (virtual hook) | **ถ้า `m_subSkeletonGraphics.Count <= 0` return ทันที**; ไม่งั้น `SetAnimation(0,name,loop,showErr:false)` + `timeScale=1` ให้ sub-skeleton ทุกตัว | :857-871 |
| `UpdateAnimationStateWithProgress` `0x060049b4` | base `SetAnimationStateWithProgress` | เหมือนข้างบน + `AnimationState.TimeScale=1`, `Update(trackEntry.AnimationEnd*progress)`, แล้ว `timeScale=timeScale` | :873-893 |
| `UpdateAnimationSpeed(ts)` `0x060049b5` | base `SetAnimationSpeed` | วน sub-skeleton แล้วตั้ง `timeScale = 1f` (**ไม่ใช้ค่า `ts`** — quirk) | :895-908 |
| `StopAnimationState(iState,progress)` `0x06004993` | `IActor.StopAnimationState` | `AnimationNameKey.Get(iState)` → `SetAnimation(0,name,loop:false)` → `TimeScale=1` → `m_SkeletonGraphic.Update(AnimationEnd*progress)` → `TimeScale=0` → `UpdateAnimationState(name,false,0)` | :283-306 |
| `SetShaderType(int)` `0x060049a0` | `IActor.SetShaderType` | **ว่างเปล่า** (IL size 1 = `ret`) — UI ไม่ทำ ZTest/renderQueue (base ทำกับ `MeshRenderer` เท่านั้น) | :443-445 |
| `DrawToBuffer(CommandBuffer)` `0x0600499f` | `UIModelShow.GenRenderTexure` | **ว่างเปล่า** (IL size 1) | :439-441 |
| `FlushSlashProperty()` `0x060049a7` | base `Update` เมื่อ lerp จบ | `Debug.LogError("UISpineActor 没有实现闪光接口")` แล้วจบ — **UI ไม่รองรับ twinkle/flash** | :551-554 |
| `Mount(...)` `0x060049aa` | `IActor.Mount` (`UIModelShow.Mount`) | `base.Mount(...)` (สร้าง/โหลด mount actor) แล้ว `m_MountActor.SeparatorSkeleton(...)`; ถ้า separator ไม่สำเร็จ → `offset.z>0 ? SetAsLastSibling : SetAsFirstSibling` | :619-633 |
| `UnMount()` `0x060049ab` | `IActor.UnMount` | `base.UnMount()` เท่านั้น (ซ่อน GO + `DoUnMount` ไม่ destroy) | :635-638 |
| `CreateMountActor()` `0x060049a8` | base `Mount` (virtual hook) | สร้าง/ใช้ `m_MountActor` เป็น **`UISpineActor`** ชื่อ `"SpineMount"` เป็นลูกของตัวเอง; copy flip X/Y, sorting layer/order, `ComputerSortIndex()` | :556-575 |
| `UpdateMount()` `0x060049ac` | base `Update` ทุกเฟรม | ถ้ามี `m_HpBone`: `canvas.referencePixelsPerUnit` (fallback 100) คูณ bone WorldX/Y แล้ว `TransformPoint` กับ SG, คง z ของ root, บวก `m_MountOffset`; ไม่มี bone → ตำแหน่ง `m_MountHpTransform.position + offset` | :640-662 |
| `SeparatorSkeleton(string[],inc,baseOrder)` `0x060049a9` | `SetSeparatorSlotName` (base), `DoMount`, `UISpineActor.Mount` | หา slot ตามชื่อ → `skeletonGraphic.separatorSlots.Add`; ถ้าได้ ≥1: `enableSeparatorSlots=true`, `allowMultipleCanvasRenderers=true`, `LateUpdate()`; reparent `SeparatorParts[j]` ไปที่ `transform.parent`, `SetSiblingIndex(baseOrder>=0 ? j+2 : j)`, `baseOrder += inc`; คืน `true` | :577-617 |
| `CreateSubSkeleton(List<SkinReq>)` `0x060049ad` (IL 511) | base `BuildBody` (เมื่อมี attach skin ที่ไม่ผูก cmpRootBone) | สร้าง `SepSGRoot` ใต้ root; `SeparatorSkeletoGraphics` ของ body; ต่อ SkinReq แต่ละตัวด้วย `CreateSubSkeletonGraphic`; ถ้า `followSlotName != "None"` → `FindBone(followSlotName)` + `BoneFollowerGraphic` ชี้ SG หลัก; `FlushTintBlack`; sort part ตาม slot index เดิม; reparent skeleton หลัก + parts ใต้ `SepSGRoot` | :664-713 |
| `CreateSubSkeletonGraphic(SkinReq,...,out bool tintBlack)` `0x060049b1` | `CreateSubSkeleton` | `CreateSkeletonGraphic(data, m_Gray, m_TintBlack)`; ตั้ง `allowMultipleCanvasRenderers=true`; เก็บเข้า `m_subSkeletonGraphics`; skin: 1 ชื่อ = `FindSkin`, หลายชื่อ = `new Skin(id)+AddSkin`; แล้ว `SeparatorSkeletoGraphics` | :761-792 |
| `SeparatorSkeletoGraphics(...)` `0x060049b2` | `CreateSubSkeleton`, `CreateSubSkeletonGraphic` | ไล่ slot ที่มี attachment เทียบ index กับ skeleton หลัก, เก็บช่วงที่ "ไม่ต่อเนื่อง" เป็นชื่อ separator slot; ถ้าได้ 1 ชิ้น → ตั้งชื่อ `"{skinID}_0_{index}"`; มากกว่า → `SetSerparatorSlotNames` + `LateUpdate` + ตั้งชื่อ `"{skinID}_{j}_{index}"`; ถ้าจำนวน part ไม่ตรง → `LogError` | :794-855 |
| `OnAttachmentLoaded(ReplaceAttachmentReq)` `0x0600498f` | `ReplaceAttachmentReq` callback (โหลด skin เสร็จ) | `GetSkeletonData`; `BuildSlotMap`; ถ้า `isSkeleton` → `LoadSkeletonAttachment`; ไม่งั้นหาชื่อ skin, ถ้า cfg `useSrcOffset` → คัดลอก `X/Y` ของ `RegionAttachment` เดิมมาทับตัวใหม่ + `UpdateOffset`; แล้ว `value.Attachment = current.Attachment`; ถ้า `SkeletonData` ไม่ตรงกับ SG → `allowMultipleCanvasRenderers=true` + `ForceUpdate()` | :181-233 |
| `ForceUpdate()` `0x06004991` | `OnAttachmentLoaded` | `SG.Update()` + `SG.Rebuild(PreRender)` ให้ main + sub ทุกตัว | :246-263 |
| `SetUseClipping(bool)` `0x06004992` | `UIModelCacheItem` (`SetUseClipping(true)`) | เก็บ flag + ตั้ง `MeshGenerator.settings.useClipping` ให้ main + sub | :265-281 |
| `GetBoneFollowerTransform(bone)` `0x0600499d` | `PlaySlotEffect` | cache `Dictionary<string,Transform>`; miss → `FindBone` + `new GameObject(boneName)` + `BoneFollowerGraphic{skeletonGraphic,boneName,bone}` parent ใต้ skeleton; ไม่พบ bone → คืน `m_RootTransform` | :401-421 |
| `PlaySlotEffect(SlotEffectInfo)` `0x0600499e` | `PlayEffectOnActor` (base) | `EffectSystemLocator.PlayUIEffectEx(id, null, canvas.sortingLayerName, canvas.sortingOrder+offset, layer)`; parent ใต้ bone follower, set local pos/rot/scale, เก็บ handle | :423-437 |
| `CreateSkeletonActor(parent,slot,bone,cmpRoot,sep,skinID)` `0x060049a2` | base `LoadSkeletonAttachmentByBone` | สร้าง `FollowSpineActor`, GameObject `f_{slot}_{skinID}` parent ใต้ `parent.transform`, `AddComponent<UISpineActor>()`, อ่าน `listenAnimEvent` จาก config | :457-471 |
| `BuildCmpBody(FollowSpineActor,orderOffset)` `0x060049a3` | base `LoadSkeletonAttachmentByBone` | `CreateRoot` → `DestroySkeleton` → `CreateSpineComponent(cmp data)` → skin → ถ้ามี separator slot → `SperatorSlot(owner.ModelRoot, name)` → `SetAnimationState(owner.StateID, owner.AnimationSpeed)` → `CreateFollow` | :473-485 |
| `SperatorSlot(root, slotName)` `0x060049a5` | `BuildCmpBody` | `allowMultipleCanvasRenderers=true`, `enableSeparatorSlots=true`, `SetSerparatorSlotNames([name])`, `LateUpdate`, then `SeparatorParts[0]` parent ใต้ `root` + `SetAsFirstSibling` | :492-505 |
| `CreateFollow(FollowSpineActor)` `0x060049a6` (IL 348) | `BuildCmpBody` | หา bone `cmpRootBone`; `AddComponent<SkeletonUtility>()` ผูก `boneRoot = m_RootTransform`; spawn bone hierarchy (`SkeletonUtilityBone.Mode.Override`) ไล่จาก root→bone (สูงสุดไม่จำกัด) ; `BoneFollowerGraphic` ชี้ SG ของ owner + `SetBone(attachBoneName)` + follow rot/scale/flip/XY/Z | :507-549 |
| `SetFlipX/SetFlipY` `0x060049ae/af` | `IActor`, `SetupSpine` | ถ้ามี `m_SepSGRoot`: ตั้ง scale ของ `SepSGRoot` เป็น (±1,±1) และ `m_SkeletonTransform.localScale = one` (กัน flip ซ้อน), ส่งต่อให้ mount actor; ไม่มี → `base.SetFlipX/Y` | :715-745 |
| `FlushTintBlack(bool)` `0x060049b0` | `CreateSubSkeleton` | OR ค่า tintBlack/canvasGroupTintBlack ของ main ด้วยสถานะ sub, ตั้ง `pmaVertexColors`, แล้ว propagate ให้ sub ทุกตัว | :747-759 |
| `RecycleSelf()` `0x060049a1` | `IActor.OnRecycle` | `base.RecycleSelf()` แล้ว reset `useClipping=false`, `updateRectTranformSize=false`, `CanvasGroupCompatible=true`, `PMAVertexColor=true`, `TintBlack=false` | :447-455 |
| `EnableUpdateRendererSize/UpdateRectTranformSize` `0x0600498a/8b` | `UIModelShow.EnableUpdateRendererSize`, `SetupSpine` | ตั้ง flag; ถ้าเปิด → `m_SkeletonGraphic.UpdateRectTransformSize()` (เมธอด**ที่เกมเพิ่มเอง**ใน SkeletonGraphic) | :61-76 |
| `EnableCanvasGroupCompatible/EnablePmaVertexColor/EnableTintBlack` `0x06004995/94/96` | `UIModelShow.InitUISpinePara` | setter ล้วน (มีผลเฉพาะตอน `CreateSkeletonGraphic` ครั้งถัดไป) | :308-321 |

### 2.2 เมธอดที่เป็น property/ctor/accessor (ไม่ต้องมี logic)

`get_s_UISpineAlwaysMult` / `set_` (`0x06004987/88`), `get_skeletonGraphic` (`0x06004989`),
`DoUpdateColor`/`UpdateColor_Imp` (`0x0600499a/9b` — set `SkeletonGraphic.color` + sub),
`FlushHudOffset` (`0x06004999` → `FlushHudOffsetImp(125f)`), `SetUpdateMode` (`0x06004998`),
`InitWithUISpineActor` (`0x060049a4`), `.ctor` (`0x060049b6`)

> 48 = 39 เมธอดที่มี logic + 9 accessor/ctor (นับจาก CSV: 48 rows)

---

## 3. `ActionSystem.SpineActor` — ส่วนที่ `UISpineActor` ใช้จริง

159 เมธอด; ที่เกี่ยวกับ UI/system นี้ (เรียงตามลำดับการทำงานจริง):

| เมธอด (token) | ผู้เรียก | พฤติกรรม |
|---|---|---|
| `LoadBody(int,int[],bool,float,...)` `0x06004909` | `UIModelCacheItem`, `GUILoadBody` | set scale/small, เรียก `m_OnLoadFinish` เก่า, `CreateRoot`, `CancelAllReq`, `StartLoadMain` |
| `GUILoadBody(...)` `0x0600490b` | `UIModelShow.SetTarget` | set `m_FadeInTime`, priority = `s_UISpineLoadPriority`, `LoadBody(isGUI:true)` |
| `StartLoadMain` `0x060048f4` (IL 422) | `LoadBody` | `dbbodyconfig` → width/height → root scale → `FlushHudOffset()`; เลือก `spineAb`/`s_spineAb` ตาม `m_IsSmall`; สร้าง `SkinReq` + attach skin reqs (ถ้า `!noCmp`); `m_BodySkinReq.Start()` |
| `SkinReq.Start/OnLoaded/Release` | ข้างบน | async AB → `SkeletionDataAssetRef.Retain()`; `Release()` = `Cancel()` หรือ `Data.Release()` (resource refcount) |
| `BuildBody` `0x06004901` (IL 849) | `StartLoadAttachSkins`/`OnAttachSkinFinish` | `CreateSpineComponent` → (override spine → SkeletonUtility) → ถ้าไม่มี attach skin: `CreateSkeletonSkinWithNoCmp`; ถ้ามี: `BuildSlotMap`, รวม skin ด้วย `AddSkin`, ผูก event, `CreateSubSkeleton(list)`; fade-in; flip; `SetAnimationState`; `SetFreezeAnimation`; `DoUpdateColor`; `FlushShadow`; `ReplaySlotEffect`; `StartLoadImageDisplay`; `StartLoadAttachment`; `SetGray`; callback; `SetupSpine` |
| `CreateRoot` `0x060048f9` | หลายจุด | `new GameObject("Root")` + `SortingGroup` + `ComputerSortIndex` |
| `SetAnimationState(id,speed,onFinish,leave,loopMode)` `0x0600491c` (IL 361) | `UIModelShow.SetTarget/SetAnimationState` | `AnimationNameKey.Get(id)` → `loop = loopMode==-1 ? (RepeatCount==-1) : (loopMode==0)`; `SetAnimation(0,name,loop)`; **เรียก virtual `UpdateAnimationState(name,loop,speed)`**; ถ้า `loopMode/LeaveTo` → ผูก `Complete` → `TrackEntryDelegate` (เปลี่ยนเป็น leave state + callback); ถ้า trackEntry null → callback + log error; ส่งต่อ follow actors |
| `SetAnimationStateWithProgress` `0x0600491d` (IL 436) | `UIModelShow` | เหมือนข้างบน + `TimeScale=1` → `AnimationState.Update(AnimationEnd*progress)` → คืน `TimeScale` |
| `StopAnimationState` `0x0600491e` | `UIModelShow.StopAnimationState` | (world) `timeScale=1` → Update(progress*end) → `timeScale=0` |
| `SetAnimationSpeed` `0x0600491f` | `UIModelShow.SetAnimationSpeed` | `AnimationState.TimeScale = speed` + virtual hook |
| `SetFreezeAnimation` `0x06004920` | `BuildBody` | `SkeletonAnimation.FreezeAnimation` |
| `SetGray(bool)` `0x0600492f` | `BuildBody`, `UIModelShow.SetGray` | `m_SkeletonAnimation.SetGray(...)` + mount actor |
| `SetColor/FadeToColor/DoUpdateColor` `0x06004930/32/4f` | `IActor` | lerp แล้วเขียนผ่าน `MaterialPropertyBlock` (`ShaderHelper.propID_Color`) ไม่แตะ material |
| `SetShaderType(int)` `0x0600493f` | `IActor` | `typo==14` → ZTest=Always, queue 3000; ไม่งั้น LessEqual/2550; ส่งต่อ mount + shadow |
| `SetShaderRenderQueue` `0x06004940` | `IActor` | เซ็ต ZTest/queue ตรง ๆ + `UpdateZTestMode` (เขียน material ทุกตัว) |
| `DrawToBuffer(cb)` `0x06004956` (IL 90) | `UIModelShow.GenRenderTexure` | `cb.DrawMesh(meshFilter.sharedMesh, renderer.localToWorldMatrix, renderer.material)` — world path เท่านั้น |
| `FlushSlashProperty` `0x06004953` (IL 81) | `Update` (twinkle lerp) | เขียน `propID_Flash`, `propID_FlashColor` ผ่าน MPB |
| `Mount/UnMount/CreateMountActor/OnMountLoadFinish/DoMount/DoUnMount/UpdateMount` `0x06004965/66/5d/5e/5f/60/61` | `IActor` | สร้าง `SpineMount` (SpineActor), `LoadBody(isGUI:false)`, `DoMount` ผูก `m_HpBone` จาก `FindBone(mountHP)`; `UnMount` = `OnRecycle()` + `SetActive(false)` (ไม่ destroy) |
| `SeparatorSkeleton` `0x06004964` | `DoMount`, `SetSeparatorSlotName` | world ใช้ `SkeletonRenderSeparator.AddToSkeletonRenderer(...)`; **base เวอร์ชันนี้ยังไม่ตั้ง `separatorSlots`** (เช็คแค่ `m_SeparatorSlotNames.Length`) |
| `SetSeparatorSlotName` `0x06004963` | `SetMountSeparatorSlotNames` | เก็บค่า + เรียก `SeparatorSkeleton` ทันที |
| `CreateSubSkeleton` `0x06004973` | `BuildBody` | **ว่างเปล่า** ใน base (UI override) |
| `CreateSkeletonActor` `0x06004976` | `LoadSkeletonAttachmentByBone` | base สร้าง `SpineActor` (mesh) |
| `BuildCmpBody` `0x06004979` | `LoadSkeletonAttachmentByBone` | base: สร้าง `SkeletonAnimation`, `SetFlipX`, sorting, `SetShaderType`, `SetAnimationState`, `CreateFollow` |
| `CreateFollow` `0x0600497b` (IL 396) | `BuildCmpBody` | `SkeletonUtility` + `SpawnBone` ไล่ hierarchy (จำกัด depth 50) + `BoneFollower` |
| `ReleaseFollowActor` `0x0600497c` | `CancelAllReq`/`RecycleSelf` | `RecycleSelf()` + `Destroy(gameObject)` ของทุก follow actor |
| `SetFollowActorAnimationState*` `0x0600497d/7e` | `SetAnimationState*` | ส่ง state ต่อให้ follow actor ที่ `listenAnimEvent == false` |
| `ChangeSlotDisplay(slot,attach,id)` `0x06004967` | `LoadSlot`, `ChangeFollowActorSlotDisplay` | `id==0` → `SetAttachment` ตรง ๆ; `id>0` → `ReplaceAttachmentReq.Start()` (โหลด AB) |
| `ChangeSlotImage(slot,icon)` `0x0600496c` | `IActor` | สร้าง `RegionAttachment` ใหม่จาก sprite + แทน `RendererObject`/UV/offset (`Replace`) |
| `RecycleSelf` `0x06004906` (IL 491) | `OnRecycle` | `UnMount`, `CleanTweener`, `CancelAllReq`, reset ทุก field, `DestroySkeleton`, stop effects, clear caches, `m_CullableObject.ClearCulling(active:true)` |
| `CancelAllReq` `0x06004904` | `LoadBody`, `RecycleSelf` | `ReleaseSkinReq` (คืน AB ref) + ยกเลิก image req + replace-attachment req + follow actors |
| `RayHit/RayHitPerspective` `0x06004915/17` | `IActor` | `SkeletonBounds` AABB + polygon hit test, ต่อ mount actor |

**ข้อสังเกตที่สำคัญสำหรับ clone (Observed):**
`UpdateAnimationState`/`UpdateAnimationStateWithProgress`/`UpdateAnimationSpeed` ใน base เป็น **empty virtual** (IL size 1)
และ `CreateSubSkeleton` base ก็ empty (IL size 1) — design ของเกมคือ "base ทำงานกับ skeleton หลัก, subclass เติมส่วน UI"

---

## 4. `UIModelShow` — 40 เมธอด

| เมธอด (token) | ถูกเรียกจาก | ทำอะไร |
|---|---|---|
| `SetTarget(modelID,scale,comps,animID,stopAnim,softMask,fadeIn,action)` `0x060002b1` | `CSGUIWidgetBase.SetChildUIModelShowTarget`, `LoadModel` (จาก `modelId` serialized) | `CreateActor` → `InitUISpinePara` → `m_OrthographicSizeScale = 1/scale` → callback เก่า → `FlushHUDTarget` → ถ้า `!stopAnim` `SetAnimationState(animID)` **ก่อนโหลด** → `GUILoadBody(...)` → ใน callback: ถ้า `stopAnim` `StopAnimationState(animID)` แล้ว `OnLoadBodyFinish` → `SetSortingLayer("Default")` |
| `CreateActor(modelID)` `0x060002af` | `SetTarget` | `InitSoftMask` → `RemoveTarget` → `IsSpineBody?` → SpineActor GO (`AddComponent<UISpineActor>`) หรือ DragonActor; `SetAsFirstSibling` |
| `OnLoadBodyFinish` `0x060002b0` | callback ของ `GUILoadBody` | callback ผู้ใช้ → `FlushHUDTarget` → `InitFollow` → `GenRenderTexure` |
| `RemoveTarget` `0x060002c4` | `CreateActor`, `OnDestroy` | `m_ActorTarget.OnRecycle()` → `Destroy(m_ActorGameObject)` → null; `m_EnableFollow=false` |
| `InitSoftMask/InitSoftMaskCommandBuffer/CreateRenderTexture/ReleaseSoftMask` `0x060002aa..ad` | `CreateActor`, `Update`, `OnDestroy` | ถ้า GameObject มี `RawImage` → `m_EnableRTT=true`, สร้าง `CommandBuffer("DragonBoneToTex")` + `RenderTexture(ARGB32)` ตาม sizeDelta × `RenderTextureScale` |
| `GenRenderTexure` `0x060002ae` | `Update` **ทุกเฟรม**, `OnLoadBodyFinish` | ถ้า RTT: resize ตาม sizeDelta, `SetRenderTarget`, clear, ตั้ง ortho view-projection relative to self, `m_ActorTarget.DrawToBuffer(cb)`, `Graphics.ExecuteCommandBuffer`, `ResolveAntiAliasedSurface` |
| `SetAnimationState/WithProgress/StopAnimationState` `0x060002b7/b8/b9` | Lua/UI | forward ไป `IActor` |
| `SetSlotAttachment` `0x060002bf` | UI | cast เป็น `UISpineActor` (ไม่ใช่ → ไม่ทำอะไร) → `SetSlotAttachment` |
| `SetSlotDisplayIndex` `0x060002be` | UI | cast เป็น `DragonBonesActor` เท่านั้น (spine: no-op) |
| `ChangeSlotDisplay/AddSkeletonSlot/ChangeFolllowActorSlotDisplay/LoadSlot` `0x060002c0..c3` | UI | forward ไป `IActor`; `LoadSlot(id>0)` = load, `id<=0` = `HideSlot` |
| `Mount/UnMount` `0x060002c9/ca` | UI | forward |
| `SetMountSeparaterSlotName` `0x060002cb` | UI | เรียกได้เฉพาะ `m_uiSpineActor` (`SetMountSeparatorSlotNames(new[]{name},inc,order)`) |
| `SetTargetFlipX/Y` `0x060002bc/bd` | UI | `SetFlipX/Y` + `FlushHUDTarget` |
| `FlushHUDTarget` `0x060002b5` | หลายจุด | `HudTarget.localPosition = GetHudOffset()` และกลับ X ถ้า flip |
| `InitFollow` `0x060002b6` | `OnLoadBodyFinish` | `FollowPairs[i].Init(m_ActorTarget)` (**เฉพาะ DragonBonesActor** — `FollowPair.Init` cast เป็น DragonBonesActor; spine = ไม่ทำงาน) |
| `EnableUpdateRendererSize` `0x060002b3` | prefab/Lua | forward ไป `UISpineActor` |
| `InitUISpinePara` `0x060002b4` | `SetTarget` | เก็บ 3 flag แล้วส่งให้ `UISpineActor` (มีผลตอนสร้าง SG) |
| `actorTarget` `0x060002a6`, `OnDestroy` `0x060002a9`, `Start/Update` `0x060002a7/a8`, `.ctor` `0x060002cc` | unity | `Start`→`LoadModel` (ถ้า `modelId>0`), `Update`→follow pairs + `GenRenderTexure` |

**Derived (สำคัญต่อการตัดสินใจ):** `GenRenderTexure` ทำงานทุกเฟรมเมื่อมี `RawImage` แต่ `UISpineActor.DrawToBuffer` ว่าง
→ เส้นทาง UI-spine จะ clear RT แล้วไม่มีอะไรถูกวาด (RT ว่าง) ส่วน dragon path วาดได้
ดังนั้น RT/CommandBuffer **ไม่ใช่ feature ของ UISpineActor** — เป็นของ `DragonBonesActor` และ world `SpineActor`

---

## 5. ตอบคำถามทีละข้อ

| คำถาม | คำตอบ (สถานะ) |
|---|---|
| `CreateSkeletonGraphic` สร้าง/กำหนดค่า component อย่างไร | สร้าง GameObject ใหม่ผ่าน `SkeletonGraphic.NewSkeletonGraphicGameObject(data, root, material)`; material เลือกจาก 4 ชุด (tintBlack × canvasGroupCompatible) ใน `GUIConfig`; ตั้ง tintBlack/canvasGroupTintBlack + additional shader channels, pmaVertexColors, allowMultipleCanvasRenderers (ตาม `s_UISpineAlwaysMult`/จำนวน atlas/material), useClipping, raycastTarget (Observed) |
| `Mount`/`UnMount` เพิ่ม/ลบ actor อย่างไร และจัดการ resource อะไร | `Mount` สร้าง child `"SpineMount"` เป็น `UISpineActor` แล้ว `LoadBody` แบบ async (SkinReq → AB refcount) — **ไม่ destroy ตอน unmount** แค่ `OnRecycle()` + `SetActive(false)`; `UnMount` ของ UI เรียก base ตรง ๆ; ตำแหน่ง HP มาจาก bone `mountHP` (Observed) |
| `UpdateAnimationState` เลือก animation/loop อย่างไร | ไม่ได้เลือกเอง — base `SetAnimationState` เลือกจาก `AnimationNameKey.Get(stateID)` (`dbanimationstate.Name`, `RepeatCount==-1`) แล้วเรียก hook นี้; override ของ UI ใช้ชื่อที่ได้ไปตั้งให้ **sub-skeleton ทุกตัว** และ return ทันทีถ้าไม่มี sub (Observed) |
| `SetSlotAttachment` หา slot และเปลี่ยน attachment แบบไหน | ไม่หา slot เอง — ส่งชื่อให้ `Skeleton.SetAttachment(slot, attach)` ของ Spine โดยตรง (ไม่มี slot map, ไม่โหลด asset, ไม่ validate) (Observed) |
| `SetGray`/`SetShaderType` เปลี่ยน material/shader อย่างไร | `SetGray` = สลับ `SkeletonGraphic.material` ระหว่าง `m_Mat`/`m_GrayMat` (material ที่ถูกกำหนดไว้ตั้งแต่สร้าง) + sub; `SetShaderType` ใน UI = **ว่าง** (base เท่านั้นที่เซ็ต ZTest/renderQueue บน `MeshRenderer`) (Observed) |
| `DrawToBuffer`/`FlushSlashProperty` เกี่ยวกับ RenderTexture/CommandBuffer ไหม | `DrawToBuffer` UI = ว่าง; world = `CommandBuffer.DrawMesh`; `FlushSlashProperty` UI = `LogError` (ไม่มี flash), base = MPB; RT อยู่ใน `UIModelShow` ไม่ใช่ actor (Observed/Derived) |
| `CreateSubSkeleton`/`SeparatorSkeleton` แยก skeleton/จัดการ slot ย่อยเมื่อไร | `CreateSubSkeleton` เกิดเมื่อ attach skin ไม่ได้ผูก `cmpRootBone` (ต้องแยก SG ต่อ skin แล้วจัดลำดับ part ตาม slot index ของ body); `SeparatorSkeleton` ใช้เมื่อต้องการแยก draw order ของ slot ที่ระบุ (mount/ชิ้นส่วน) โดย reparent `SeparatorParts` และตั้ง sibling index ตาม `baseSortingOrder` (Observed) |

---

## 6. Unknown / ยังไม่มีหลักฐาน (ห้ามเดา)

1. **ชื่อ Shader จริงของ `GUIConfig.uiSpineMat*`** — เป็น material asset ที่ serialize อยู่นอก DLL (ไม่พบใน E1/E2)
   ยืนยันได้แค่ "มี 8 ฟิลด์ material: Mat/GrayMat × tintBlack × canvasGroupCompatible"
2. **ค่า default ของ `s_UISpineAlwaysMult` / `s_UISpineLoadPriority`** — เป็น `[AppDataModelOption]` ที่โหลดจาก player prefs/config
   ไม่ใช่ค่าคงที่ใน IL
3. **ค่าตั้งต้น `m_SeparatorBaseSortingOrder`** — field ไม่มี initializer ใน C# (0) แต่ `RecycleSelf` เขียน 5 แล้วทับด้วย 0
   (น่าจะเป็น bug เดิม) — พฤติกรรมจริงขึ้นกับ serialized value: **Unknown**
4. **ผลของ `useSrcOffset` กับ attachment ที่ไม่ใช่ `RegionAttachment`** — โค้ด cast ตรง ๆ (`(RegionAttachment)`) จึงจะ throw
   ถ้าเป็น mesh attachment: ยังไม่มีหลักฐานการเกิดจริง (Unknown)
5. **`FollowPair` กับ UISpineActor** — `FollowPair.Init` รองรับเฉพาะ `DragonBonesActor`; จะมีผลกับ spine ไหม = ไม่มี (Observed ว่าไม่ทำ),
   แต่เหตุผลเชิง design = Unknown
6. **`SkeletonGraphic` ที่เกมใช้เป็น fork** — มี `SetSerparatorSlotNames` / `SeparatorParts` / `UpdateRectTransformSize`
   ที่ **ไม่มีใน spine-unity 3.8 มาตรฐาน** (ตรวจ `my_clone\Assets\Spine` แล้ว) → clone ต้อง implement เอง (ดู design)
7. **URP/Built-in ของโปรเจกต์เดิม** และ shader ที่ใช้จริงใน build — Unknown (ระดับ asset)
8. **`UpdateAnimationSpeed` ที่ทิ้งค่า `ts`** — ตั้ง `timeScale=1` เสมอ; เจตนา Unknown (อาจกัน sub-skeleton เพี้ยน)

---

## 7. ภาคผนวก: หลักฐานเมธอดว่าง (IL size = 1)

| Type.Method | token | IL size |
|---|---|---|
| `UISpineActor.DrawToBuffer` | `0x0600499f` | 1 |
| `UISpineActor.SetShaderType` | `0x060049a0` | 1 |
| `SpineActor.UpdateAnimationState` | `0x06004921` | 1 |
| `SpineActor.UpdateAnimationStateWithProgress` | `0x06004922` | 1 |
| `SpineActor.UpdateAnimationSpeed` | `0x06004923` | 1 |
| `SpineActor.CreateSubSkeleton` | `0x06004973` | 1 |
| `SpineActor.CheckCulling` | `0x06004948` | 1 |
| `SpineActor.SetAnimation(bool)` | `0x06004949` | 1 |
| `SpineActor.InitWithActor` | `0x0600497a` | 1 |

เมธอดว่างเหล่านี้ยืนยันว่า "48 เมธอด" ไม่ได้แปลว่าต้อง implement 48 อย่าง — หลายตัวเป็น hook ที่ subclass เติม
