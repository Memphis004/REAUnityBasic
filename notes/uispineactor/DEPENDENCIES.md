# DEPENDENCIES — `ActionSystem.UISpineActor`

เอกสารนี้แยก dependency เป็นชั้น ๆ เพื่อให้ประเมินได้ว่า clone ต้องมีอะไรจริง
และอะไรเป็นของเกมที่ต้องแทนด้วย adapter

สถานะ: **Observed** จาก source/metadata เว้นแต่ระบุ; ช่องว่างกับ spine-unity 3.8
ตรวจจาก `my_clone/Assets/Spine` (stock) เทียบกับ `extracted/ilspy/spine-unity` (fork ของเกม)

---

## 1. ภาพรวมชั้น dependency

```
UISpineActor
├─ SpineActor (base)  ── IActor, lifecycle, config, AB, mount, follow, effect
├─ Spine.Unity (fork)
│   ├─ SkeletonGraphic + CanvasRenderer / RawImage
│   ├─ SkeletonDataAsset / AtlasAsset / MeshGenerator.Settings
│   ├─ Skeleton / Skin / Slot / Bone / Attachment (Region/Mesh)
│   ├─ AnimationState / TrackEntry / UpdateMode
│   ├─ BoneFollowerGraphic / SkeletonUtility / SkeletonUtilityBone
│   └─ (fork-only) SetSerparatorSlotNames / SeparatorParts / UpdateRectTransformSize
├─ เกม
│   ├─ CSGUI.GUIConfig (material 8 ตัว)
│   ├─ ConfigData.DragonBonesConfig (dbbodyconfig / dbcomponentcfg)
│   ├─ ActorAnimator.AnimationNameKey (dbanimationstate)
│   ├─ SpineSurport.SkinReq / SkeletionDataAssetRef
│   ├─ ActionSystem.ReplaceAttachmentReq / FollowSpineActor
│   ├─ EffectSystem.EffectSystemLocator / EffectObject
│   ├─ Common.AppDataModelOption
│   └─ UnityEngine.Rendering.SortingGroup (ผ่าน base)
└─ Unity
    ├─ MonoBehaviour / GameObject / Transform / Material / Color
    ├─ Canvas / CanvasRenderer / RectTransform / CanvasUpdate
    ├─ CommandBuffer (เฉพาะ signature)
    └─ Debug / Mathf / Vector3 / Quaternion
```

---

## 2. Hard dependency (ขาดไม่ได้ ถ้าจะให้พฤติกรรมตรง)

### 2.1 `Spine.Unity.SkeletonGraphic`

ใช้เป็น renderer หลักแทน `SkeletonAnimation`

| สมาชิกที่ใช้ | ใช้ที่ | หมายเหตุ |
|---|---|---|
| `NewSkeletonGraphicGameObject(data, parent, material)` | `CreateSkeletonGraphic` | parent = `m_RootTransform`; ชื่อ default `"New Spine GameObject"` |
| `skeletonDataAsset` | `CreateSkeletonGraphic`/error log | |
| `MeshGenerator.settings.tintBlack` | create/flush | |
| `MeshGenerator.settings.canvasGroupTintBlack` | create/flush | |
| `MeshGenerator.settings.pmaVertexColors` | create/flush | |
| `MeshGenerator.settings.useClipping` | create/`SetUseClipping` | |
| `allowMultipleCanvasRenderers` | create/`SetupSpine`/`SperatorSlot`/`SeparatorSkeleton`/sub | |
| `enableSeparatorSlots` | `SperatorSlot`/`SeparatorSkeleton`/sub | |
| `separatorSlots` (`List<Slot>`) | `SeparatorSkeleton` | |
| `SeparatorParts` (`List<Transform>`) | `DestroySkeleton`/`SperatorSlot`/`SeparatorSkeleton`/sub | |
| `SetSerparatorSlotNames(string[])` | `SperatorSlot`/`SeparatorSkeleton`/sub | **fork-only** |
| `UpdateRectTransformSize()` | `UpdateRectTranformSize` | **fork-only** |
| `LateUpdate()` | `SperatorSlot`/`SeparatorSkeleton`/sub | เรียกตรงเพื่อ rebuild parts |
| `Update()` | `ForceUpdate` | advance 1 frame |
| `Rebuild(CanvasUpdate.PreRender)` | `ForceUpdate` | |
| `Update(float deltaTime)` | `StopAnimationState` | |
| `color` | `UpdateColor_Imp` | `Graphic.color` (vertex color) |
| `material` | `SetGray` | material กลางจาก `GUIConfig` |
| `raycastTarget` | `CreateSkeletonGraphic` | ค่าเป็น false เสมอ |
| `canvas` | `PlaySlotEffect`/`UpdateMount` | `Graphic.canvas` (อาจ null) |
| `updateWhenInvisible` | `CreateSpineComponent`/`SetUpdateMode` | `UpdateMode` |
| `Skeleton` / `SkeletonData` / `AnimationState` | ทั่วไป | |
| `timeScale` | animation hooks | คนละตัวกับ `AnimationState.TimeScale` |
| `transform` / `gameObject` | parent/destroy | |

### 2.2 Spine runtime types

| ชนิด | ใช้ที่ | ต้องมีอะไร |
|---|---|---|
| `SkeletonDataAsset` | สร้าง SG / อ่าน atlas | `atlasAssets`, `GetSkeletonData(quiet)` |
| `AtlasAsset` | ตัดสิน multi-renderer | `MaterialCount` |
| `Skeleton` | slot/bone/skin | `Slots`, `FindSlot`, `FindBone`, `SetAttachment`, `SetSkin`, `Data` |
| `Skin` | sub-skeleton | `GetAttachments()`, `Attachments.Count`, `AddSkin`, ctor(name) |
| `Skin.SkinEntry` | `OnAttachmentLoaded` | `Attachment` |
| `Slot` | separator/slot map | `Data.Name`, `Data.Index`, `Attachment`, `Bone.Data.Name` |
| `Bone` | mount/follow | `WorldX`, `WorldY`, `Parent`, `Data.Name` |
| `Attachment` | slot attachment | base type |
| `RegionAttachment` | `useSrcOffset` | `X`, `Y`, `UpdateOffset()` |
| `AnimationState` | play/stop | `SetAnimation`, `TimeScale`, `Update`, `Event` |
| `TrackEntry` | play/stop | `AnimationEnd`, `Complete` |
| `UpdateMode` | update mode | `Nothing=0`, `OnlyAnimationStatus=1`, `EverythingExceptMesh=2`, `FullUpdate=3`, `OnlyEventTimelines=4` |
| `SkeletonBounds` | ray hit (base) | ไม่เกี่ยวกับ UI โดยตรง |

### 2.3 Spine.Unity components

| ชนิด | ใช้ที่ | สมาชิก |
|---|---|---|
| `BoneFollowerGraphic` | `GetBoneFollowerTransform`, `CreateFollow`, `CreateSubSkeleton` | `skeletonGraphic`, `boneName`, `bone`, `SetBone`, `followBoneRotation`, `followLocalScale`, `followSkeletonFlip`, `followXYPosition`, `followZPosition` |
| `SkeletonUtility` | `CreateFollow` | `boneRoot`, `SpawnBone(bone, parent, Mode.Override, pos, rot, sca)` |
| `SkeletonUtilityBone.Mode` | `CreateFollow` | `Override` |

### 2.4 เกม: `CSGUI.GUIConfig`

`GUIConfig : MonoBehaviour`, singleton แบบ field `public static GUIConfig Instance`
(ไม่ใช่ `Singleton<T>`); ฟิลด์ material ที่ UI ใช้ (Serialized):

| ฟิลด์ | ใช้เมื่อ |
|---|---|
| `uiSpineMat` | `!tintBlack && canvasGroupCompatible` |
| `uiSpineGrayMat` | เหมือนข้างบน (gray) |
| `uiSpineMatTintBlack` | `tintBlack && canvasGroupCompatible` |
| `uiSpineGrayMatTintBlack` | เหมือนข้างบน (gray) |
| `uiSpineMatNoGroup` | `!tintBlack && !canvasGroupCompatible` |
| `uiSpineGrayMatNoGroup` | เหมือนข้างบน (gray) |
| `uiSpineMatTintBlackNoGroup` | `tintBlack && !canvasGroupCompatible` |
| `uiSpineGrayMatTintBlackNoGroup` | เหมือนข้างบน (gray) |

**ข้อกำหนดของ clone:** material เหล่านี้เป็น asset กลาง **ห้าม destroy**
และต้องถือเป็น shared material (อย่า mutate โดยไม่สั่งการ)

### 2.5 เกม: config / service

| service | signature ที่ใช้ | ใช้ที่ |
|---|---|---|
| `Singleton<DragonBonesConfig>.Instance()` | `GetDBBodyConfig(int)`, `GetDBComponentcfg(int)`, `IsSpineBody(int)` | โหลด/สร้าง/sub/follow |
| `DragonBonesConfig.GetDBBodyConfig` → `dbbodyconfig` | `size`, `spineAb`, `s_spineAb`, `spineSkins`, `cmps`, `noCmp`, `overrideSpine`, `headPos`, `bottomPos` | `StartLoadMain`, `FlushHudOffset`, `BuildBody` |
| `DragonBonesConfig.GetDBComponentcfg` → `dbcomponentcfg` | `spineSkins`, `spineAb`, `s_spineAb`, `followSlot`, `cmpRootBone`, `useSrcOffset`, `listenAnimEvent` | attach skin / follow |
| `AnimationNameKey.Get(int)` | → `dbanimationstate{Name,RepeatCount,LeaveTo}` | animation ทุกเส้น |
| `SkinReq` | ctor + `Start/Cancel/Release/OnLoaded`, `Data`, `SkinNames`, `AssetAbName`, `followSlotName`, `cmpRootBoneName`, `skinID` | โหลด AB |
| `SkeletionDataAssetRef` | `Retain()`, `Release()`, `skeletonDataAsset` | refcount |
| `ReplaceAttachmentReq` | `skinReq`, `slotName`, `strParams`, `isSkeleton`, `onLoadFinish` | slot เปลี่ยน attachment |
| `FollowSpineActor` | 7 fields สาธารณะ | follow actor |
| `EffectSystemLocator.effectManager` | `PlayUIEffectEx(id, null, layerName, order, layer)` | `PlaySlotEffect` |
| `EffectObject` | `transform`, `Handle` | effect |
| `AppDataModelOption` | attribute บน static property | `s_UISpineAlwaysMult`, `s_UISpineLoadPriority`, shader types |
| `AppDataModel.RegOption` | key `option.ActionSystem.UISpineActor.s_UISpineAlwaysMult` | โหลดค่า config |

### 2.6 Unity API

| API | ใช้ที่ |
|---|---|
| `MonoBehaviour` / `GameObject` / `Transform` | ทุกที่ |
| `Object.Destroy` | destroy SG/sub/SepSGRoot |
| `Transform.SetParent(parent, worldPositionStays)` | reparent ทุกแบบ |
| `Transform.SetSiblingIndex` / `SetAsFirstSibling` / `SetAsLastSibling` | separator/mount/follow |
| `Material` | gray/normal |
| `Canvas` (`sortingLayerName`, `sortingOrder`, `referencePixelsPerUnit`) | effect/mount |
| `CanvasRenderer` | ภายใน SkeletonGraphic |
| `RectTransform` (`sizeDelta`, `pivot`) | ผ่าน fork `UpdateRectTransformSize` |
| `CanvasUpdate.PreRender` | `ForceUpdate` |
| `CommandBuffer` | signature `DrawToBuffer` (ไม่ใช้จริง) |
| `SortingGroup` | base `CreateRoot`; UI disable |
| `Color`, `Vector3`, `Quaternion`, `Mathf` | ทั่วไป |
| `Debug.LogError` | error paths |

---

## 3. Dependency ที่เป็น "สัญญาณ" ไม่ใช่ hard requirement

| ของ | เหตุผล |
|---|---|
| `MeshRenderer` / `MeshFilter` / `MaterialPropertyBlock` | base ใช้; UI set null และไม่ใช้ |
| `SkeletonAnimation` | base field; UI ไม่สร้าง |
| `SkeletonRenderSeparator` | base `SeparatorSkeleton`; UI ไม่ใช้ |
| `SkeletonBounds` | base raycast; UI ใช้ผ่าน base |
| `ShadowNode` / `ActorLightInfo` / `CustomLightManager` | base world lighting; UI ไม่มี mesh → no-op |
| `DG.Tweening` | base color/move tween; UI ไม่เรียกตรง |
| `SortingGroup` sorting values | UI copy ไป mount แต่ disable ตัวเอง |
| `CommandBuffer.DrawMesh` | UI `DrawToBuffer` ว่าง |

---

## 4. ช่องว่างกับ spine-unity 3.8 มาตรฐาน (สำคัญมาก)

ตรวจจาก `my_clone/Assets/Spine` (stock) เทียบ `extracted/ilspy/spine-unity` (fork เกม)

| API ของเกม | stock 3.8 | ผลต่อ clone |
|---|---|---|
| `SkeletonGraphic.SetSerparatorSlotNames(string[])` | **ไม่มี** (มี `separatorSlotNames` แบบ protected + ใช้ตอน initialize) | ต้องเพิ่ม method คล้ายกัน (set names + reapply) |
| `SkeletonGraphic.UpdateRectTransformSize()` | **ไม่มี** (มี `MatchRectTransformWithBounds()`) | ต้องเพิ่ม wrapper หรือใช้ `MatchRectTransformWithBounds` |
| `SkeletonGraphic.separatorSlots` | มี (`readonly List<Slot>`) | ใช้ได้ |
| `SkeletonGraphic.SeparatorParts` | มี | ใช้ได้ |
| `SkeletonGraphic.enableSeparatorSlots` | มี | ใช้ได้ |
| `SkeletonGraphic.allowMultipleCanvasRenderers` | มี | ใช้ได้ |
| `SkeletonGraphic.Update()` / `Update(float)` | มี | ใช้ได้ |
| `BoneFollowerGraphic` + fields | มี | ใช้ได้ |
| `SkeletonUtility.boneRoot` / `SpawnBone` | มี | ใช้ได้ |
| `MeshGenerator.Settings` (`tintBlack`, `canvasGroupTintBlack`, `pmaVertexColors`, `useClipping`) | มี | ใช้ได้ |
| `AnimationState.SetAnimation(int,string,bool,bool showErr)` | **ไม่มี** — stock เป็น 3 พารามิเตอร์และ **throw** เมื่อไม่พบ animation | ต้อง guard `HasAnimation`/`FindAnimation` เอง หรือเพิ่ม overload |
| `Skeleton.SetAttachment` เมื่อ slot/attachment ไม่มี | **throw** ทั้งคู่ (เหมือน fork) | clone ต้องตัดสินใจ: compat = ปล่อย throw; ปรับปรุง = return false + warning |
| `SkeletonGraphic.freeze` / `AnimationState.TimeScale` | มี | ใช้แทน `SetFreezeAnimation` ของ UI |
| `SkeletonGraphic.material` / `color` / `raycastTarget` | มี | ใช้ได้ |

**สรุป:** clone ที่ใช้ spine-unity 3.8 ต้องมี "fork shim" อย่างน้อย 2 อย่าง
(`SetSerparatorSlotNames`, `UpdateRectTransformSize`) และต้องจัดการ `SetAnimation`
ให้ไม่ throw ในเส้นทางที่เกมคาดว่าจะได้ null + optional error log

---

## 5. Dependency ด้านข้อมูล (config fields)

### 5.1 `dbbodyconfig`

| ฟิลด์ | ใช้ที่ | ผล |
|---|---|---|
| `spineAb` / `s_spineAb` | `StartLoadMain`, `AddAttachmentSkinReq` | เลือก AB ตาม `m_IsSmall` |
| `spineSkins` | `SkinReq` | ชื่อ skin |
| `cmps` | `StartLoadMain` | attach skin IDs |
| `noCmp` | `StartLoadMain` | ถ้า true + ส่ง attach → error log |
| `overrideSpine` | `BuildBody` | spawn `SkeletonUtility` hierarchy |
| `size` | `StartLoadMain` | width/height (scale) |
| `headPos` / `bottomPos` | `FlushHudOffsetImp` | HUD offset (UI ×125) |
| `shadow` | base `FlushShadow` | ไม่เกี่ยวกับ UI |

### 5.2 `dbcomponentcfg`

| ฟิลด์ | ใช้ที่ |
|---|---|
| `spineAb` / `s_spineAb` | `SkinReq` (attach) |
| `spineSkins` | `SkinReq` |
| `followSlot` / `cmpRootBone` | สร้าง follow/sub |
| `useSrcOffset` | `OnAttachmentLoaded` (copy RegionAttachment X/Y) |
| `listenAnimEvent` | `CreateSkeletonActor` (follow actor) |
| `slots` | ใช้โดยระบบอื่น (ไม่ใช้ใน UI ตรง ๆ) |

### 5.3 `dbanimationstate`

| ฟิลด์ | ใช้ที่ |
|---|---|
| `Name` | `SetAnimationState`, `StopAnimationState` |
| `RepeatCount` | ตัดสิน `loop` (`== -1`) |
| `LeaveTo` | ผูก `Complete` |
| `FadeTime` | ไม่ใช้ใน UI (อาจใช้ที่อื่น) |

---

## 6. Nullability / failure points ที่ต้องออกแบบรองรับ

| จุด | ความเสี่ยง | หลักฐาน |
|---|---|---|
| `GUIConfig.Instance` | null → NRE ตอนสร้าง SG | field static |
| material 8 ตัว | null → SG ได้ material null | serialized |
| `GetDBComponentcfg(skinID)` | null → NRE ใน `CreateSkeletonActor` | ไม่มี null check |
| `DragonBonesConfig.GetDBBodyConfig` | null → error log + ไม่โหลด | base |
| `m_SkeletonGraphic.canvas` | null → NRE ใน `PlaySlotEffect` | ไม่มี check |
| `m_ISkeleton` | null → NRE ใน `GetBoneFollowerTransform`/`CreateFollow` | เรียกหลัง build เท่านั้น |
| `skeletonDataAsset` | null → NRE | ผู้เรียก garant |
| `Skin` ที่หาไม่พบ | `SetSkin(null)` หรือ `Skin.Attachments` NRE | `CreateSubSkeletonGraphic`/`SeparatorSkeletoGraphics` |
| `RegionAttachment` cast | mesh attachment → `InvalidCastException` | `OnAttachmentLoaded` |
| `m_MountActor` | null → NRE ใน `Mount` ถ้า `CreateMountActor` ล้มเหลว | base |
| `m_subSkeletonGraphics` | มี null ได้ระหว่าง destroy | loop มี null check บางจุด |

---

## 7. สรุปสิ่งที่ clone ต้องมี (ขั้นต่ำ)

1. **Spine runtime + SkeletonGraphic** ที่มี API ครบตาม §2.1 (หรือ shim 2 ตัว)
2. **Material provider** 8 ช่อง (normal/gray × tintBlack × canvasGroup) — ห้าม destroy
3. **Config provider** สำหรับ body/component/animation (แทน `DragonBonesConfig` + `AnimationNameKey`)
4. **Async skin loader** ที่มี refcount/cancel (แทน `SkinReq` + `SkeletionDataAssetRef`)
5. **Effect service** ที่เล่น effect ตาม Canvas sorting (แทน `EffectSystemLocator.PlayUIEffectEx`)
6. **Mount/follow data model** (`FollowSpineActor`-like) และ `ReplaceAttachmentReq`-like callback
7. **Canvas/RectTransform** context สำหรับ `referencePixelsPerUnit`, `sizeDelta`, `pivot`
8. **ตัวเลือก:** `AppDataModelOption`-like config สำหรับ `s_UISpineAlwaysMult`

> ไม่จำเป็นต้องมี `CommandBuffer`, `MeshRenderer`, `SkeletonAnimation`, `SortingGroup` ที่ active
> สำหรับ UI path — แต่ base class ของ clone อาจยังต้องมีเพื่อใช้ร่วม world path
