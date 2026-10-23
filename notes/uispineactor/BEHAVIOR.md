# BEHAVIOR — `ActionSystem.UISpineActor`

เอกสารนี้เป็นสเปกพฤติกรรมรายเมธอดของ `UISpineActor` ทั้ง 48 เมธอด
พร้อมพฤติกรรมที่สืบทอดจาก `SpineActor` ที่ subclass ต้องพึ่ง
ทุกข้อความเป็น **Observed** เว้นแต่ระบุไว้เป็นอย่างอื่น

อ้างอิงซอร์ส decompile (อ่านเท่านั้น ไม่คัดลอก):
`extracted/ilspy/Assembly-CSharp/ActionSystem/UISpineActor.cs`,
`.../SpineActor.cs`, `.../UIModelShow.cs`, `.../UIModelCache/UIModelCacheItem.cs`,
`extracted/ilspy/spine-unity/Spine.Unity/SkeletonGraphic.cs`

---

## 1. Type identity และ state

```
ActionSystem.SpineActor : UnityEngine.MonoBehaviour, ActorSystem.IActor
└─ ActionSystem.UISpineActor          TypeDef 0x02000643
```

### 1.1 ฟิลด์ของ `UISpineActor` (12 ตัว)

| ฟิลด์ | ชนิด | ค่าเริ่มต้น | ความหมาย |
|---|---|---|---|
| `m_SkeletonGraphic` | `SkeletonGraphic` | `null` | renderer หลัก (แทน `SkeletonAnimation`) |
| `m_useClipping` | `bool` | `true` | ใช้ clipping ของ `MeshGenerator` |
| `m_GrayMat` | `Material` | `null` | material gray ที่เลือกจาก `GUIConfig` |
| `m_Mat` | `Material` | `null` | material ปกติที่เลือกจาก `GUIConfig` |
| `m_RaycastTarget` | `bool` | `false` | **ไม่มีการเขียนค่าในทั้งคลาส** |
| `m_CanvasGroupCompatible` | `bool` | `true` | เลือกคู่ material + `canvasGroupTintBlack` |
| `m_PMAVertexColor` | `bool` | `true` | `MeshGenerator.settings.pmaVertexColors` |
| `m_TintBlack` | `bool` | `false` | เปิด tint-black ตอนสร้าง SG |
| `m_updateRectTranformSize` | `bool` | `false` | เปิด auto-resize RectTransform |
| `m_SepSGRoot` | `Transform` | `null` | root ของ sub-skeleton + separator parts |
| `m_subSkeletonGraphics` | `List<SkeletonGraphic>` | new list | sub-skeleton ทั้งหมด |
| `s_UISpineAlwaysMult` | `static bool` | `false` (IL) | บังคับ multi-CanvasRenderer; ผูก `AppDataModelOption` |

Nested type: `UISpineActor.IndexTransform` (public class) — `int index`, `Transform t`,
ctor `(int i, Transform transform)`; ใช้เรียงลำดับ part ตาม slot index

### 1.2 ฟิลด์/ค่าจาก base ที่มีผลกับ UI

`m_RootTransform`, `m_SkeletonTransform`, `m_ISkeleton`, `m_IAnimationState`,
`m_SortingGroup`, `m_SlotMap`, `m_FollowerTransform`, `m_FollowActors`,
`m_MountActor`, `m_MountHP`, `m_MountOffset`, `m_MountHpTransform`, `m_HpBone`,
`m_IsMounting`, `m_SeparatorSlotNames`, `m_SeparatorOrtingOrderIncreament` (=5),
`m_SeparatorBaseSortingOrder` (=0), `m_InvisbleUpdateMode`, `m_Gray`, `m_FlipX`,
`m_FlipY`, `m_StateID`, `m_AnimationSpeed`, `m_UnityColor`, `m_FadeInAlpha`,
`m_BodySkinReq`, `m_AttachSkinReqs`, `m_BodyCfg`

> `m_SkeletonAnimation`, `m_MeshRenderer`, `m_MeshFilter` ยังมีอยู่ใน base แต่ UI **ไม่ใช้**
> (`m_MeshRenderer`/`m_MeshFilter` ถูก set `null` ใน `DestroySkeleton`)

---

## 2. เมธอดทั้ง 48

> เนื้อหา §2.1–§2.8 เรียงตาม **ลำดับการอธิบาย** (จัดกลุ่มตามหน้าที่)
> และมี **1 หัวข้อ = 1 เมธอด** ตั้งแต่ข้อ 1 ถึง 48
> ส่วนตาราง §2.0 เรียงตาม **ลำดับ token** เพื่อใช้ตรวจกับ CSV
> คอลัมน์สุดท้ายของตารางบอกว่าทุกเมธอดไปอยู่หัวข้อไหน (48/48)

### 2.0 ดัชนี 48 เมธอด

| # (token) | token | เมธอด | IL | § (เอกสาร) |
|---:|---|---|---:|---:|
| 1 | `0x06004987` | `get_s_UISpineAlwaysMult` | 6 | §2.1 → 1 |
| 2 | `0x06004988` | `set_s_UISpineAlwaysMult` | 7 | §2.1 → 2 |
| 3 | `0x06004989` | `get_skeletonGraphic` | 7 | §2.1 → 3 |
| 4 | `0x0600498a` | `EnableUpdateRendererSize` | 22 | §2.1 → 4 |
| 5 | `0x0600498b` | `UpdateRectTranformSize` | 34 | §2.1 → 5 |
| 6 | `0x0600498c` | `DestroySkeleton` | 338 | §2.2 → 11 |
| 7 | `0x0600498d` | `CreateSkeletonGraphic` | 434 | §2.2 → 9 |
| 8 | `0x0600498e` | `CreateSpineComponent` | 131 | §2.2 → 10 |
| 9 | `0x0600498f` | `OnAttachmentLoaded` | 367 | §2.2 → 12 |
| 10 | `0x06004990` | `SetupSpine` | 51 | §2.2 → 13 |
| 11 | `0x06004991` | `ForceUpdate` | 95 | §2.3 → 14 |
| 12 | `0x06004992` | `SetUseClipping` | 115 | §2.3 → 15 |
| 13 | `0x06004993` | `StopAnimationState` | 219 | §2.3 → 16 |
| 14 | `0x06004994` | `EnablePmaVertexColor` | 8 | §2.1 → 6 |
| 15 | `0x06004995` | `EnableCanvasGroupCompatible` | 8 | §2.1 → 7 |
| 16 | `0x06004996` | `EnableTintBlack` | 8 | §2.1 → 8 |
| 17 | `0x06004997` | `SetSlotAttachment` | 33 | §2.3 → 17 |
| 18 | `0x06004998` | `SetUpdateMode` | 95 | §2.3 → 18 |
| 19 | `0x06004999` | `FlushHudOffset` | 12 | §2.3 → 19 |
| 20 | `0x0600499a` | `DoUpdateColor` | 104 | §2.3 → 20 |
| 21 | `0x0600499b` | `UpdateColor_Imp` | 78 | §2.3 → 21 |
| 22 | `0x0600499c` | `SetGray` | 108 | §2.3 → 22 |
| 23 | `0x0600499d` | `GetBoneFollowerTransform` | 126 | §2.3 → 23 |
| 24 | `0x0600499e` | `PlaySlotEffect` | 152 | §2.3 → 24 |
| 25 | `0x0600499f` | `DrawToBuffer` | 1 | §2.3 → 25 |
| 26 | `0x060049a0` | `SetShaderType` | 1 | §2.3 → 26 |
| 27 | `0x060049a1` | `RecycleSelf` | 42 | §2.4 → 27 |
| 28 | `0x060049a2` | `CreateSkeletonActor` | 130 | §2.4 → 28 |
| 29 | `0x060049a3` | `BuildCmpBody` | 118 | §2.4 → 29 |
| 30 | `0x060049a4` | `InitWithUISpineActor` | 22 | §2.4 → 30 |
| 31 | `0x060049a5` | `SperatorSlot` | 116 | §2.4 → 31 |
| 32 | `0x060049a6` | `CreateFollow` | 348 | §2.4 → 32 |
| 33 | `0x060049a7` | `FlushSlashProperty` | 11 | §2.4 → 33 |
| 34 | `0x060049a8` | `CreateMountActor` | 152 | §2.5 → 34 |
| 35 | `0x060049a9` | `SeparatorSkeleton` | 228 | §2.5 → 35 |
| 36 | `0x060049aa` | `Mount` | 94 | §2.5 → 36 |
| 37 | `0x060049ab` | `UnMount` | 7 | §2.5 → 37 |
| 38 | `0x060049ac` | `UpdateMount` | 239 | §2.5 → 38 |
| 39 | `0x060049ad` | `CreateSubSkeleton` | 511 | §2.6 → 41 |
| 40 | `0x060049ae` | `SetFlipX` | 124 | §2.5 → 39 |
| 41 | `0x060049af` | `SetFlipY` | 94 | §2.5 → 40 |
| 42 | `0x060049b0` | `FlushTintBlack` | 204 | §2.7 → 44 |
| 43 | `0x060049b1` | `CreateSubSkeletonGraphic` | 275 | §2.6 → 42 |
| 44 | `0x060049b2` | `SeparatorSkeletoGraphics` | 499 | §2.6 → 43 |
| 45 | `0x060049b3` | `UpdateAnimationState` | 98 | §2.7 → 45 |
| 46 | `0x060049b4` | `UpdateAnimationStateWithProgress` | 130 | §2.7 → 46 |
| 47 | `0x060049b5` | `UpdateAnimationSpeed` | 82 | §2.7 → 47 |
| 48 | `0x060049b6` | `.ctor` | 39 | §2.8 → 48 |

> คอลัมน์ **§ (เอกสาร)** คือเลขหัวข้อในเอกสารนี้ ซึ่งเรียงตามลำดับการอธิบาย
> และมี **1 หัวข้อ = 1 เมธอด** (1–48 ครบ ไม่มีหัวข้อรวมกลุ่ม)
> คอลัมน์ **# (token)** คือลำดับ token/IL ซึ่งใช้ตรวจกับ CSV
> สองลำดับนี้ต่างกัน เพราะเอกสารจัดกลุ่มตามหัวข้อ (สร้าง renderer, attachment, mount ฯลฯ)
> ไม่ได้เรียงตาม token

รูปแบบแต่ละหัวข้อ: `token` · access modifier · IL size · ผู้เรียกหลัก · พฤติกรรม

### 2.1 Properties, accessors และคอนฟิกก่อนสร้าง (1–8)

#### 1. `static bool s_UISpineAlwaysMult { get; }`
- `0x06004987` · public static · IL 6 (getter อัตโนมัติ)
- ถูกผูกใน `AppDataModel.RegOption("option.ActionSystem.UISpineActor.s_UISpineAlwaysMult", ...)`
  โดยใช้ setter §2
- ใช้เฉพาะใน `CreateSkeletonGraphic` (§9) เพื่อบังคับ `allowMultipleCanvasRenderers`
- ค่าจริงใน player prefs/config = **Unknown** (ดู `UNKNOWNS.md`)

#### 2. `static bool s_UISpineAlwaysMult { set; }`
- `0x06004988` · public static · IL 7 (setter อัตโนมัติ)
- ไม่มี logic เพิ่ม; เก็บค่าลง static field

#### 3. `override SkeletonGraphic skeletonGraphic => m_SkeletonGraphic`
- `0x06004989` · public · IL 7
- base ประกาศ `virtual SkeletonGraphic skeletonGraphic => null`; UI คืนตัวหลัก
- ใช้โดย `Mount`/`UpdateMount`/`SeparatorSkeleton` ภายใน และ `UIModelCacheItem`

#### 4. `void EnableUpdateRendererSize(bool enable)`
- `0x0600498a` · public · IL 22
- ผู้เรียก: `UIModelShow.EnableUpdateRendererSize` (prefab/Lua)
- เก็บ flag; ถ้า `true` เรียก `UpdateRectTranformSize()` (§5) ทันที
- ไม่คงอยู่ข้ามการ recycle — `RecycleSelf` (§27) reset เป็น `false`

#### 5. `void UpdateRectTranformSize()`
- `0x0600498b` · protected · IL 34 (สะกด `Tranform` ตามของจริง)
- ผู้เรียก: `EnableUpdateRendererSize` (§4), `SetupSpine` (§13)
- ถ้า flag เปิดและ `m_SkeletonGraphic != null` → `m_SkeletonGraphic.UpdateRectTransformSize()`
- เมธอดปลายทางเป็น **ของเกมที่เพิ่มใน fork**: `UpdateMesh()` + `MatchRectTransformSingle/MultipleRenderer()`
  → เขียน `RectTransform.sizeDelta` และ `pivot` (side effect ต่อ layout)

#### 6. `void EnablePmaVertexColor(bool flag)`
- `0x06004994` · public · IL 8
- ผู้เรียก: `UIModelShow.InitUISpinePara`
- setter ล้วน **ไม่มีผลกับ SG ที่สร้างแล้ว** — มีผลตอน `CreateSkeletonGraphic` (§9) ครั้งถัดไป
- `EnablePmaVertexColor(true)` อาจถูก override เป็น `false` ในเส้นทาง non-tint + canvas-group (ดู §9)

#### 7. `void EnableCanvasGroupCompatible(bool flag)`
- `0x06004995` · public · IL 8
- setter ล้วน; มีผล 2 จุดตอนสร้าง SG: เลือกคู่ material และตั้ง `canvasGroupTintBlack`
- ไม่ปรับ SG ที่มีอยู่แล้ว

#### 8. `void EnableTintBlack(bool flag)`
- `0x06004996` · public · IL 8
- setter ล้วน; ใช้เป็นค่า `tintBlack` ตอน `CreateSkeletonGraphic` (§9) ครั้งถัดไป
- ไม่เรียก `FlushTintBlack` (§44) ให้เอง

### 2.2 การสร้าง/ทำลาย renderer (9–13)

#### 9. `SkeletonGraphic CreateSkeletonGraphic(SkeletonDataAsset skeletonDataAsset, bool gray, bool tintBlack = false)`
- `0x0600498d` · **internal** · IL 434
- ผู้เรียก: `CreateSpineComponent`, `CreateSubSkeletonGraphic`
- ลำดับ:
  1. อ่าน `skeletonDataAsset.blendModeMaterials` แล้วทิ้ง (ไม่ใช้)
  2. เลือก material 4 ชุดตาม `tintBlack` × `m_CanvasGroupCompatible` จาก `GUIConfig.Instance`:

     | tintBlack | canvasGroupCompatible | `m_Mat` | `m_GrayMat` |
     |---|---|---|---|
     | true | true | `uiSpineMatTintBlack` | `uiSpineGrayMatTintBlack` |
     | true | false | `uiSpineMatTintBlackNoGroup` | `uiSpineGrayMatTintBlackNoGroup` |
     | false | true | `uiSpineMat` | `uiSpineGrayMat` |
     | false | false | `uiSpineMatNoGroup` | `uiSpineGrayMatNoGroup` |

  3. **ผลข้างเคียงสำคัญ:** เขียนทับ `m_Mat`/`m_GrayMat` ของ actor ทุกครั้งที่เรียก
     → หลังสร้าง sub-skeleton หลายตัว ค่าสุดท้ายคือของ sub ตัวท้าย (ดู §4.3)
  4. `SkeletonGraphic.NewSkeletonGraphicGameObject(data, m_RootTransform, gray ? m_GrayMat : m_Mat)`
     → GameObject ชื่อ `"New Spine GameObject"` (default), parent = `m_RootTransform`
  5. ถ้า tintBlack:
     - `MeshGenerator.settings.tintBlack = true`
     - `settings.canvasGroupTintBlack = m_CanvasGroupCompatible`
     - ถ้า `SG.canvas != null` → `canvas.additionalShaderChannels |= TexCoord1 | TexCoord2`
     - ถ้าไม่ tintBlack → `settings.canvasGroupTintBlack = false`
  6. `settings.pmaVertexColors = m_PMAVertexColor`
     (แต่ในเส้นทาง `!tintBlack && m_CanvasGroupCompatible` ค่า `m_PMAVertexColor` ถูกบังคับเป็น `false`
     ก่อนหน้านี้แล้ว)
  7. `allowMultipleCanvasRenderers = s_UISpineAlwaysMult || atlasAssets.Length > 1
     || (atlasAssets.Length == 1 && atlasAssets[0].MaterialCount > 1)`
  8. `settings.useClipping = m_useClipping`
  9. `raycastTarget = m_RaycastTarget` → **false เสมอ** (ไม่มีการเขียน `m_RaycastTarget`)
- ไม่เรียก `Initialize()` เอง; `NewSkeletonGraphicGameObject` → `AddSkeletonGraphicComponent` เรียกให้
- `gray` มีผลแค่วันแรกที่สร้าง: เลือก material เริ่มต้น; หลังจากนั้น `SetGray` ใช้ `m_Gray`/`m_Mat`

#### 10. `override void CreateSpineComponent(SkeletonDataAsset skeletonDataAsset)`
- `0x0600498e` · protected · IL 131
- ผู้เรียก: base `BuildBody` และ `BuildCmpBody` ของ UI (virtual dispatch)
- `DestroySkeleton()` → `CreateSkeletonGraphic(data, m_Gray, m_TintBlack)`
- `m_SkeletonGraphic.updateWhenInvisible = m_InvisbleUpdateMode`
- ผูก `m_ISkeleton` / `m_IAnimationState` / `m_SkeletonTransform` (โค้ดเขียนซ้ำ 2 รอบ)
- **`m_SortingGroup.enabled = false`** — UI ไม่ใช้ SortingGroup; การเรียงทำด้วย sibling/Canvas
- ไม่มี `SetParent` (factory ทำแล้ว), ไม่มี `Initialize` (factory ทำแล้ว)

#### 11. `override void DestroySkeleton()`
- `0x0600498c` · protected · IL 338
- ผู้เรียก: `CreateSpineComponent`, `BuildCmpBody`, base `RecycleSelf` (virtual)
- ถ้า `m_SkeletonTransform != null`:
  1. เอา `m_SkeletonGraphic.SeparatorParts` ทั้งหมด `SetParent(m_SkeletonGraphic.transform)`
     (คืน part ให้ SG ก่อน destroy — ไม่ destroy part เอง)
  2. destroy sub-skeleton ทุกตัว + `m_subSkeletonGraphics.Clear()`
  3. `Destroy(m_SkeletonTransform.gameObject)` (SG + part ที่เพิ่ง reparent)
  4. set `m_SkeletonTransform = null`, `m_IAnimationState = null`, `m_ISkeleton = null`,
     `m_MeshRenderer = null`, `m_MeshFilter = null`, `m_SkeletonGraphic = null`
- loop ที่ 2 (`m_subSkeletonGraphics.Count > 0`) เป็น **dead code** ในเส้นทางปกติ
  เพราะถูก clear ไปแล้วในบล็อกแรก; ยังมีประโยชน์เฉพาะกรณี `m_SkeletonTransform == null`
  แต่ list ยังไม่ว่าง
- ถ้ามี `m_SepSGRoot` → destroy GameObject + set null
- `m_SlotMap.Clear()` เสมอ
- **ไม่** clear `m_FollowerTransform`, **ไม่** stop slot effect, **ไม่** destroy material
  (material เป็นของ `GUIConfig`)

#### 12. `override void OnAttachmentLoaded(ReplaceAttachmentReq slotReq)`
- `0x0600498f` · protected · IL 367
- ผู้เรียก: callback ของ `ReplaceAttachmentReq` (base `StartLoadAttachment`/`ReplaceAttachment`)
- `var data = slotReq.skinReq.Data.skeletonDataAsset.GetSkeletonData(quiet:false)`; `null` → return
- `BuildSlotMap()`
- ถ้า `slotReq.isSkeleton` → `LoadSkeletonAttachment(slotReq.skinReq, slotReq.slotName, null,
  slotReq.strParams, m_SlotMap)` แล้ว return (สร้าง follow/sub-skeleton)
- ไม่งั้น:
  - `useSrcOffset` จาก `DragonBonesConfig.GetDBComponentcfg(skinReq.skinID)`
  - วน `skinReq.SkinNames`:
    - ถ้า `m_SlotMap[slotReq.slotName]` มี: `FindSkin(name)`; เอา attachment **ตัวแรก** ของ skin
    - ถ้า `useSrcOffset`: cast **ทั้งสอง** attachment เป็น `RegionAttachment`,
      copy `X`/`Y` จากของเดิมไปของใหม่ แล้ว `UpdateOffset()`
      → ถ้าเป็น `MeshAttachment` จะ `InvalidCastException` (**Observed จากชนิด cast**)
    - `value.Attachment = current.Attachment`
  - หลังแต่ละชื่อ: ถ้า `m_SkeletonGraphic.SkeletonData != data` (reference)
    → `allowMultipleCanvasRenderers = true` + `ForceUpdate()`
    → **`ForceUpdate` อยู่ในลูป** จึงอาจเรียกหลายครั้ง
- slot ที่ไม่มีใน map = no-op เงียบ

#### 13. `override void SetupSpine()`
- `0x06004990` · protected · IL 51
- ผู้เรียก: ท้าย `SpineActor.BuildBody` (virtual hook)
- `UpdateRectTranformSize()`
- ถ้า `m_AttachSkinReqs.Count > 0` → `skeletonGraphic.allowMultipleCanvasRenderers = true`
- `DoHideSlots()` (base)
- `SetFlipX(m_FlipX)` (UI override; ดู §39)

### 2.3 Attachment / update / color / material (14–26)

#### 14. `void ForceUpdate()`
- `0x06004991` · public · IL 95
- ผู้เรียก: `OnAttachmentLoaded`, `UIModelCacheItem.CaptureImageOnNextFrame`
- ถ้า `m_SkeletonGraphic == null` return
- `m_SkeletonGraphic.Update()` → **advance animation 1 frame** (`Update(Time.deltaTime)` หรือ unscaled)
- `m_SkeletonGraphic.Rebuild(CanvasUpdate.PreRender)` → สร้าง mesh ใหม่
- วน sub-skeleton ที่ไม่ null: `Update()` + `Rebuild(PreRender)`
- **ไม่ side-effect free** — ใช้ก่อน capture cache หรือหลังสลับ attachment

#### 15. `void SetUseClipping(bool use)`
- `0x06004992` · public · IL 115
- ผู้เรียก: `UIModelCacheItem.SetTarget` (`SetUseClipping(true)`), public API
- เก็บ flag; ถ้า SG null return
- set `MeshGenerator.settings.useClipping` ให้ main + sub
- ไม่ rebuild mesh — ค่ามีผลตอน generate ครั้งถัดไป

#### 16. `override void StopAnimationState(int iState, float progress = 0f)`
- `0x06004993` · public · IL 219
- ผู้เรียก: `UIModelShow.StopAnimationState` / callback กรณี `stopAnim`, `UIModelCacheItem`
- ถ้า `m_IAnimationState == null` return
- `AnimationNameKey.Get(iState)`; ถ้า `null` → จบ (ไม่บันทึก state)
- `SetAnimation(0, name, loop:false)` (fork: `showErr` default = true)
- `m_IAnimationState.AnimationState.TimeScale = 1f`
- ถ้า `trackEntry != null`: `m_SkeletonGraphic.Update(trackEntry.AnimationEnd * progress)`;
  `AnimationState.TimeScale = 0f`
- ถ้า `null`: `Debug.LogError("【name】[asset] not found Animation: ...")`
  — **ไม่ reset TimeScale กลับเป็น 0** ในเส้นทางนี้
- สุดท้าย `UpdateAnimationState(name, loop:false, 0f)` → hook ของ UI (§45) ตั้ง sub-skeleton
  `timeScale = 1f`
- **ต่างจาก base:** ไม่ตั้ง `m_StateID = iState` และไม่ตั้ง `m_AnimationSpeed = 0f`
  → `StateID`/`AnimationSpeed` ยังเป็นค่าเดิม และ follow actor ไม่ถูก stop ตาม

#### 17. `void SetSlotAttachment(string slotName, string attachName)`
- `0x06004997` · public · IL 33
- ผู้เรียก: `UIModelShow.SetSlotAttachment` (public wrapper) ซึ่งถูกเรียกจาก `CSGUIWidgetBase` อีกที
- ถ้า `m_SkeletonGraphic == null` → **no-op เงียบ** (guard เดียวของเมธอด)
- ไม่งั้นเรียก `m_SkeletonGraphic.Skeleton.SetAttachment(slotName, attachName)`
- **ไม่มี** `try/catch`, ไม่ rebuild, ไม่เรียก `ForceUpdate`
  → mesh จะอัปเดตใน `LateUpdate`/`Rebuild` รอบถัดไปของผู้เรียก
- **ไม่มี** guard สำหรับ sub-skeleton — เปลี่ยนเฉพาะ skeleton หลัก
- อ้างอิงพฤติกรรม Spine: `Skeleton.SetAttachment` throw ถ้าไม่พบ slot
  หรือไม่พบ attachment ใน slot/skin ปัจจุบัน (ตรงกับ fork และ stock 3.8 — ดู `DEPENDENCIES.md`)
  → ชื่อผิดทำให้เกิด exception ทะลุถึงผู้เรียก
- ต่างจาก `OnAttachmentLoaded` (§12) ที่ resolve skin/attachment เองและจัดการ offset;
  เมธอดนี้เป็น passthrough ตรง ๆ

#### 18. `override void SetUpdateMode(int updateMode)`
- `0x06004998` · public · IL 95
- แปลงตรง ๆ `m_InvisbleUpdateMode = (UpdateMode)updateMode` (ไม่ clamp/validate)
- ถ้า SG null return; set `updateWhenInvisible` ให้ main + sub
- Base เรียกบน mount actor ตอน `OnMountLoadFinish` ด้วย `(int)m_InvisbleUpdateMode`
- `UIModelCacheItem` เรียก `SetUpdateMode(3)` = `UpdateMode.FullUpdate`

#### 19. `override void FlushHudOffset()`
- `0x06004999` · protected · IL 12
- ผู้เรียก: base `StartLoadMain` หลังตั้ง `m_RootTransform.localScale`
- `FlushHudOffsetImp(125f)` — UI ใช้ตัวคูณ 125 (base ใช้ 1)
- คำนวณจาก `m_BodyCfg.headPos`/`bottomPos` × `m_Scale` × 125

#### 20. `override void DoUpdateColor()`
- `0x0600499a` · protected · IL 104
- ผู้เรียก: base `BuildBody`, base `UpdateColor(deltaTime)` เมื่อ lerp เปลี่ยน
- `c = m_UnityColor.Value; c.a *= m_FadeInAlpha.Value; UpdateColor_Imp(c)`
- แล้ววน `m_FollowActors.Values` → `followActor.UpdateColor_Imp(c)`
- **ไม่ premultiply RGB** (base ทำ `r/g/b *= a`) — ตั้งใจเพราะ UI ใช้ vertex color + shader

#### 21. `override void UpdateColor_Imp(Color col)`
- `0x0600499b` · public · IL 78
- ถ้า SG null return
- `m_SkeletonGraphic.color = col`; วน sub ที่ไม่ null: `sub.color = col`
- ไม่แตะ material/MPB/shadow

#### 22. `override void SetGray(bool gray)`
- `0x0600499c` · public · IL 108
- ผู้เรียก: base `BuildBody` (`SetGray(m_Gray)`), `UIModelShow.SetGray`
- `m_Gray = gray`; ถ้า SG null return
- `mat = gray ? m_GrayMat : m_Mat`
- `m_SkeletonGraphic.material = mat`; วน sub: `sub.material = mat`
- **ผลข้างเคียง:** เพราะ `m_Mat`/`m_GrayMat` ถูกเขียนทับทุกครั้งที่ `CreateSkeletonGraphic`
  (รวม sub) การ `SetGray` หลังมี sub อาจใช้ material ของ sub ตัวท้ายกับทุกตัว
  (โดยเฉพาะเมื่อ tintBlack ต่างกัน)
- ไม่เปลี่ยน `MeshGenerator.settings.pmaVertexColors` หรือ tintBlack ตาม material

#### 23. `override Transform GetBoneFollowerTransform(string boneName)`
- `0x0600499d` · protected · IL 126
- ผู้เรียก: `PlaySlotEffect` (และ base `ReplaySlotEffect` ผ่าน virtual)
- cache `m_FollowerTransform`: hit → คืนค่า
- miss: `m_ISkeleton.Skeleton.FindBone(boneName)`
  - พบ → `new GameObject(boneName)` + `BoneFollowerGraphic`
    (`skeletonGraphic = m_SkeletonGraphic`, `boneName`, `bone`) parent ใต้ `m_SkeletonTransform`;
    เก็บ cache; คืน transform
  - ไม่พบ → คืน `m_RootTransform` (**ไม่ cache**)
- ถ้า `m_ISkeleton == null` → NRE

#### 24. `override void PlaySlotEffect(SlotEffectInfo info)`
- `0x0600499e` · protected · IL 152
- ผู้เรียก: base `PlayEffectOnActor` → `PlaySlotEffect` (virtual), base `ReplaySlotEffect`
- `canvas = m_SkeletonGraphic.canvas` — **ไม่เช็ค null** (ถ้าไม่มี Canvas → NRE)
- `EffectSystemLocator.effectManager.PlayUIEffectEx(info.EffectID, null,
  canvas.sortingLayerName, canvas.sortingOrder + info.OrderOffset, gameObject.layer)`
- ถ้า effect ไม่ null: parent ใต้ bone follower (`worldPositionStays:false`),
  `localPosition = info.Offset`, `localRotation = identity`, `localScale = info.Scale`,
  `info.Handle = effect.Handle`
- ต่างจาก base: base ใช้ `PlayEffect(...)` แบบ 3D world; UI ใช้ `PlayUIEffectEx` ตาม sorting

#### 25. `override void DrawToBuffer(CommandBuffer commandBuffer)`
- `0x0600499f` · public · IL **1** → ว่างเปล่า
- `UIModelShow.GenRenderTexure` เรียกทุกเฟรมเมื่อ host มี `RawImage`
  → UI-spine จะ clear RT แต่ไม่มีอะไรถูกวาด (RT ว่าง)
- RT/CommandBuffer เป็น feature ของ `DragonBonesActor`/world `SpineActor` ไม่ใช่ UI

#### 26. `override void SetShaderType(int typo)`
- `0x060049a0` · public · IL **1** → ว่างเปล่า
- UI ไม่ตั้ง ZTest/renderQueue (base ทำกับ `MeshRenderer` เท่านั้น)

### 2.4 Effects / recycle / follow actor (27–33)

#### 27. `override void RecycleSelf()`
- `0x060049a1` · protected · IL 42
- ผู้เรียก: base `OnRecycle` (public) → `RecycleSelf()`
- `base.RecycleSelf()` ก่อน (ซึ่งเรียก virtual `UnMount()`, `DestroySkeleton()`,
  `ReleaseFollowActor()` ตามลำดับของ base) แล้ว reset:
  `m_useClipping = false`, `m_updateRectTranformSize = false`,
  `m_CanvasGroupCompatible = true`, `m_PMAVertexColor = true`, `m_TintBlack = false`
- **ไม่** reset `m_RaycastTarget` (ไม่มีใครเขียน), `m_Mat`/`m_GrayMat` (คงค่าเดิม)
- หลัง recycle: `m_useClipping` เปลี่ยนจาก default `true` → `false`
  → การ `LoadBody` ครั้งถัดไปจะไม่ clipping จนกว่าจะเรียก `SetUseClipping(true)`

#### 28. `override FollowSpineActor CreateSkeletonActor(SpineActor parent, string followSlotName, string targetBone, string cmpRootBone, string separaterSlotName, int skinID)`
- `0x060049a2` · protected · IL 130
- ผู้เรียก: base `LoadSkeletonAttachmentByBone` เมื่อยังไม่มี follow actor ของ slot นั้น
- สร้าง `FollowSpineActor`; set `ownerActor = parent`, `separatorSlotName`
- `m_FollowActors.Add(followSlotName, fs)`
- GameObject `"f_{followSlotName}_{skinID}"` parent ใต้ **`base.transform`** (ไม่ใช่ `Root`)
- `attachBoneName = targetBone`, `cmpRootBoneName = cmpRootBone`
- `AddComponent<UISpineActor>()`
- `listenAnimEvent = DragonBonesConfig.GetDBComponentcfg(skinID).listenAnimEvent`
  — **ไม่เช็ค null**
- ต่างจาก base: base parent ใต้ `parent.m_RootTransform` และใช้ `SpineActor` (mesh)

#### 29. `override void BuildCmpBody(FollowSpineActor fsActor, int orderOffset = 0)`
- `0x060049a3` · public · IL 118
- ผู้เรียก: base `LoadSkeletonAttachmentByBone`
- `CreateRoot()` → `DestroySkeleton()` → `CreateSpineComponent(cmpBodySkinReq.Data.skeletonDataAsset)`
  → `CreateSkeletonSkinWithNoCmp(m_ISkeleton, SkinNames)`
- ถ้า `separatorSlotName` ไม่ว่าง → `SperatorSlot(fsActor.ownerActor.ModelRoot, name)`
- `InitWithUISpineActor(fsActor.ownerActor as UISpineActor)` → `SetAnimationState(owner.StateID, owner.AnimationSpeed)`
- `CreateFollow(fsActor)`
- **ละเว้น `orderOffset`** และไม่เรียก `SetFlipX`/`SetSortingLayer`/`SetSortingOrder`/`SetShaderType`
  (base ทำทั้งหมดนี้) — UI ใช้ sibling/Canvas และ copy flip ผ่าน mount/ separators แทน
- ถ้า `ownerActor` ไม่ใช่ `UISpineActor` → `as` คืน null → NRE ใน `InitWithUISpineActor`

#### 30. `private void InitWithUISpineActor(UISpineActor otherActor)`
- `0x060049a4` · private · IL 22
- `SetAnimationState(otherActor.m_StateID, otherActor.m_AnimationSpeed)`
- เป็นวิธี sync animation ของ follow actor กับ owner (ไม่แตะ progress)

#### 31. `protected void SperatorSlot(Transform root, string separatorSlotName)` (สะกดตามจริง)
- `0x060049a5` · protected · IL 116
- ผู้เรียก: `BuildCmpBody`
- `allowMultipleCanvasRenderers = true`; `enableSeparatorSlots = true`;
  `SetSerparatorSlotNames(new[]{name})`; `LateUpdate()` (ให้ fork สร้าง parts)
- ถ้า `SeparatorParts.Count > 0`: `parts[0].SetParent(root, worldPositionStays:false)`;
  `SetAsFirstSibling()`
- ใช้ root = `owner.ModelRoot` (transform ของ actor เจ้าของ) — คนละแบบกับ
  `SeparatorSkeleton` ที่ reparent ไป `transform.parent`

#### 32. `override void CreateFollow(FollowSpineActor fsActor)`
- `0x060049a6` · protected · IL 348
- ผู้เรียก: `BuildCmpBody`
- `bone = m_ISkeleton.Skeleton.FindBone(fsActor.cmpRootBoneName)`
- ถ้าไม่พบ → `Debug.LogError(Data.Name + "不存在骨骼" + fsActor.attachBoneName)`
  (**log ชื่อ attachBoneName ไม่ใช่ cmpRootBoneName** — ข้อความอาจชี้ผิด bone)
- ถ้าพบ:
  - `SkeletonUtility` เพิ่มบน GameObject ของ SG หลัก; `boneRoot = m_RootTransform`
  - สร้าง list จาก bone ไล่ `Parent` ขึ้นไปจน null (ไม่มี limit)
  - ถ้า `list.Count >= 2`:
    - spawn ancestor บนสุด (`list[^1]`) ใต้ `m_SkeletonGraphic.transform`
    - วน `num = list.Count-2` ลงถึง 1: spawn ใต้ตัวก่อนหน้า
    - spawn bone เป้าหมาย (`list[0]`) ใต้ตัวก่อนหน้า
  - ถ้า `< 2`: spawn bone เป้าหมายใต้ SG transform
  - `BoneFollowerGraphic` บน GameObject สุดท้าย:
    `skeletonGraphic = ((UISpineActor)fsActor.ownerActor).m_SkeletonGraphic`,
    `SetBone(fsActor.attachBoneName)`,
    `followBoneRotation/followLocalScale/followSkeletonFlip/followXYPosition/followZPosition = true`
- `SkeletonUtilityBone.Mode.Override`, `pos/rot/sca = true`
- cast owner เป็น `UISpineActor` ตรง ๆ → ถ้า owner เป็น `SpineActor` ธรรมดาจะ throw

#### 33. `override void FlushSlashProperty()`
- `0x060049a7` · public · IL 11
- `Debug.LogError("UISpineActor 没有实现闪光接口")`
- base `Update` เรียกเมื่อ `m_Slash` lerp จบ → UI ไม่รองรับ twinkle/flash
  (ยังเรียก `Twinkle` ได้ แต่จะไม่เห็นผลและมี error log)

### 2.5 Mount และ flip (34–40)

#### 34. `override void CreateMountActor()`
- `0x060049a8` · protected · IL 152
- ผู้เรียก: base `Mount`
- ถ้า `m_MountActor == null`: `new GameObject("SpineMount")` parent ใต้ `base.transform`;
  `AddComponent<UISpineActor>()`
- ถ้ามีแล้ว: cast เป็น `UISpineActor`
- `CreateRoot()`
- copy `m_FlipX`, `m_FlipY`
- copy `m_SortingGroup.sortingLayerName` / `sortingOrder` (แม้ SG จะ disable SortingGroup)
- `ComputerSortIndex()`

#### 35. `override bool SeparatorSkeleton(string[] sSlotName, int sortingOrderIncreament, int baseSortingOrder)`
- `0x060049a9` · public · IL 228
- ผู้เรียก: `UISpineActor.Mount`, base `DoMount`, base `SetSeparatorSlotName` (virtual)
- ถ้า `skeletonGraphic == null` หรือ `sSlotName.Length == 0` → `false`
- **ถ้า `separatorSlots.Count > 0` → คืน `true` ทันที** (ไม่สนชื่อชุดใหม่)
- `separatorSlots.Clear()` (ตามด้วย loop หา slot)
- หา slot จาก `skeleton.FindSlot(name)`; เก็บเฉพาะที่พบ
- ถ้าได้ ≥1:
  - `enableSeparatorSlots = true`; `allowMultipleCanvasRenderers = true`
  - `LateUpdate()` (fork: update mesh + separator part parents)
  - `parent = base.transform.parent`; `order = baseSortingOrder`
  - วน `SeparatorParts[j]`: `SetParent(parent)` (**worldPositionStays = true**),
    `SetSiblingIndex(order >= 0 ? j + 2 : j)`; `order += sortingOrderIncreament`
  - คืน `true`
- ถ้าไม่พบ slot → `false`
- หมายเหตุ: `baseSortingOrder < 0` → ใช้ index `j` (0-based) ไม่บวก 2

#### 36. `override void Mount(int mountBodyID, int[] mountSlots, string mountHP, float scale, Vector3 offset, Action onMountFinish)`
- `0x060049aa` · public · IL 94
- `base.Mount(...)`:
  - `CreateMountActor()`
  - เก็บ `m_MountHP`, `m_MountOffset`, `m_OnMountFinish`
  - `m_MountActor.LoadBody(mountBodyID, mountSlots, isGUI:false, scale, false, OnMountLoadFinish)`
  - **return ทันที** (async)
- แล้วเรียก `m_MountActor.SeparatorSkeleton(m_SeparatorSlotNames,
  m_SeparatorOrtingOrderIncreament, m_SeparatorBaseSortingOrder)`
- ถ้า `false`: `offset.z > 0 ? SetAsLastSibling() : SetAsFirstSibling()` บน transform ของ mount
- **timing:** ตอนนี้ mount ยังโหลดไม่เสร็จ (`m_SkeletonGraphic == null`) → `SeparatorSkeleton`
  คืน `false` แทบทุกครั้ง → fallback sibling ทำงาน
  งานแยก part จริงเกิดภายหลังใน base `OnMountLoadFinish` → `DoMount` →
  `m_MountActor.SeparatorSkeleton(...)` อีกครั้ง (ตอนนั้น SG พร้อมแล้ว)
  → ผลลัพธ์สุดท้าย: reparent parts ไป `mountActor.transform.parent` (= actor หลัก)
  พร้อม sibling index ตาม `m_SeparatorBaseSortingOrder`
- ถ้า mount actor โหลดเสร็จก่อนเรียก `Mount` (กรณี reuse) การเรียกครั้งแรกอาจสำเร็จ

#### 37. `override void UnMount()`
- `0x060049ab` · public · IL 7
- `base.UnMount()` เท่านั้น
- base: `m_MountActor?.OnRecycle()`; ถ้า `m_IsMounting` → set false, `DoUnMount()`
  (clear bone/hp transform, reset root localPosition), `m_MountActor.gameObject.SetActive(false)`
- **ไม่ destroy** mount actor — ใช้ซ้ำได้

#### 38. `override void UpdateMount()`
- `0x060049ac` · protected · IL 239
- ผู้เรียก: base `Update` ทุกเฟรม (ถ้า `m_ISkeleton != null`)
- ถ้า `!m_IsMounting || m_MountHpTransform == null` return
- ถ้ามี `m_HpBone`:
  - `canvas = skeletonGraphic.canvas`, ถ้า null → `skeletonGraphic.GetComponentInParent<Canvas>()`
  - `num = canvas != null ? canvas.referencePixelsPerUnit : 100f`
  - `v = m_MountActor.skeletonGraphic.transform.TransformPoint(
    new Vector3(m_HpBone.WorldX * num, m_HpBone.WorldY * num, 0))`
  - `v.z = m_MountActor.RootTransform.position.z`
  - `m_RootTransform.position = v + m_MountOffset`
- ไม่งั้น: `m_RootTransform.position = m_MountHpTransform.position + m_MountOffset`
- ต่างจาก base: base ใช้ `WorldX/WorldY` ตรง ๆ; UI คูณ `referencePixelsPerUnit`
  (Spine world unit → Canvas unit)

#### 39. `override void SetFlipX(bool flip)`
- `0x060049ae` · public · IL 124
- `m_FlipX = flip`
- ถ้ามี `m_SepSGRoot`:
  - ถ้า `m_SkeletonTransform != null` → `localScale = Vector3.one` (กัน flip ซ้อน)
  - `m_SepSGRoot.localScale = (flipX ? -1 : 1, flipY ? -1 : 1, 1)`
  - `m_MountActor?.SetFlipX(flip)`
- ไม่งั้น → `base.SetFlipX(flip)` (scale ที่ `m_SkeletonTransform`)
- ใช้ `m_FlipY` ปัจจุบันในการคำนวณแกน Y ด้วย

#### 40. `override void SetFlipY(bool flip)`
- `0x060049af` · public · IL 94
- `m_FlipY = flip`
- ถ้ามี `m_SepSGRoot`:
  - `m_SepSGRoot.localScale = (flipX ? -1 : 1, flipY ? -1 : 1, 1)`
  - `m_MountActor?.SetFlipY(flip)`
- ไม่งั้น → `base.SetFlipY(flip)`
- **asymmetry:** ไม่ reset `m_SkeletonTransform.localScale = one` ก่อน (ต่างจาก `SetFlipX`)
  → ถ้าเคย flip ตอนไม่มี SepSGRoot แล้วภายหลังมี root อาจมี scale ค้าง

### 2.6 Sub-skeleton / separator (41–43)

#### 41. `override void CreateSubSkeleton(List<SkinReq> skinReqs)`
- `0x060049ad` · protected · IL 511
- ผู้เรียก: base `BuildBody` เมื่อมี attach skin ที่มี `followSlotName` แต่ไม่มี `cmpRootBoneName`
- สร้าง `slotIndexDict: name → Data.Index` จาก `m_SkeletonGraphic.Skeleton.Slots`
  (ชื่อซ้ำ → `ArgumentException`)
- ถ้า `m_SepSGRoot == null`: `new GameObject("SepSGRoot")` parent ใต้ `m_RootTransform`
- `SeparatorSkeletoGraphics(m_BodySkinReq, m_SkeletonGraphic, slotIndexDict, transformIndexes)`
  (แยก part ของ body เอง)
- วน `skinReqs`:
  - `CreateSubSkeletonGraphic(req, dict, indexes, out tintBlack)`
  - parent sub ใต้ `m_SepSGRoot`
  - `flag |= tintBlack`
  - ถ้า `followSlotName != "None"`: `FindBone` บน **skeleton หลัก**;
    ถ้าพบ add `BoneFollowerGraphic` บน sub: `followBoneRotation/followLocalScale/
    followSkeletonFlip/followZPosition/followXYPosition = true`,
    `skeletonGraphic = m_SkeletonGraphic` (main), `boneName`, `bone`
- `FlushTintBlack(flag)` (§44)
- `transformIndexes.Sort((a,b) => a.index.CompareTo(b.index))`
- `m_SkeletonTransform.SetParent(m_SepSGRoot, worldPositionStays:false)`
- วน index: `t.SetParent(m_SepSGRoot, false)`, `SetSiblingIndex(num)`
- ผลลัพธ์ลำดับ draw: main SG + sub/part ทั้งหมดเป็นลูกของ `SepSGRoot` เรียงตาม slot index
- หมายเหตุ: `m_SepSGRoot` ไม่ถูกสร้างใหม่ถ้ามีอยู่แล้ว (reuse ข้าม build ภายในอายุ actor)

#### 42. `protected SkeletonGraphic CreateSubSkeletonGraphic(SkinReq req, Dictionary<string,int> slotIndexDict, List<IndexTransform> transformIndexs, out bool tintBlack)`
- `0x060049b1` · protected · IL 275
- `data = req.Data.skeletonDataAsset`
- `SG = CreateSkeletonGraphic(data, m_Gray, m_TintBlack)`
- `tintBlack = SG.MeshGenerator.settings.tintBlack` (out)
- `updateWhenInvisible = m_InvisbleUpdateMode`; `allowMultipleCanvasRenderers = true`
- `m_subSkeletonGraphics.Add(SG)`
- ถ้า `req.SkinNames.Count > 0`:
  - 1 ชื่อ → `FindSkin(name)`
  - หลายชื่อ → `new Skin($"{req.skinID}")` + `AddSkin` เฉพาะที่พบ
  - `SG.Skeleton.SetSkin(skin)` (อาจเป็น null ถ้าชื่อเดียวและหาไม่พบ)
- `SeparatorSkeletoGraphics(req, SG, slotIndexDict, transformIndexs)`
- คืน SG
- **ผลข้างเคียง:** เรียก `CreateSkeletonGraphic` → เขียนทับ `m_Mat`/`m_GrayMat` (§9)

#### 43. `private void SeparatorSkeletoGraphics(SkinReq req, SkeletonGraphic target, Dictionary<string,int> slotIndexDict, List<IndexTransform> transformIndexs)`
- `0x060049b2` · private · IL 499
- ถ้า `target.Skeleton.Skin.Attachments.Count <= 0` → return
- วน slot ของ sub (ตามลำดับ):
  - ข้าม slot ที่ `Attachment == null`
  - ตัวแรกที่มี attachment และมีใน dict → `num = index`, `list.Add(index)`
  - ตัวถัด ๆ ไปที่มีใน dict:
    - ถ้า `index - num != 1` → `list.Add(index)`, `list2.Add(slot.Data.Name)`
    - `num = index`
- ถ้า `list.Count == 1`:
  - ตั้งชื่อ GO เป็น `"{skinID}_0_{index}"`
  - `transformIndexs.Add(new IndexTransform(index, target.transform))`
  - **ไม่** เปิด separator slots
- ไม่งั้น:
  - `allowMultipleCanvasRenderers = true`; `enableSeparatorSlots = true`
  - `SetSerparatorSlotNames(list2.ToArray())`; `LateUpdate()`
  - `parts = target.SeparatorParts`
  - ถ้า `parts.Count == list.Count`: ตั้งชื่อ `"{skinID}_{j}_{list[j]}"` + add IndexTransform
  - ไม่งั้น `Debug.LogError($"{req.AssetAbName} 拆分错误 {parts.Count} {transformIndexs.Count}")`
- `list2` เก็บชื่อ slot ที่ **เริ่มช่วงใหม่** (ไม่รวมชิ้นแรก) — จำนวน part จึงควร = `list.Count`
- ถ้า attachment ต่อเนื่องกันหมด (ไม่ขาดตอน) → `list.Count == 1` → ใช้ SG เดียว ไม่แยก

### 2.7 Tint-black และ animation hooks (44–47)

#### 44. `private void FlushTintBlack(bool subState)`
- `0x060049b0` · private · IL 204
- ผู้เรียก: `CreateSubSkeleton` (§41) หลังสร้าง sub ทุกตัว
- `main.settings.tintBlack |= subState`
- `main.settings.canvasGroupTintBlack |= subState`
- `main.settings.pmaVertexColors = m_PMAVertexColor`
- `subState = main.settings.tintBlack` (ค่าที่ OR แล้ว)
- วน sub ทุกตัว: `tintBlack = subState`, `canvasGroupTintBlack = subState`,
  `pmaVertexColors = m_PMAVertexColor`
- ผล: ถ้า sub ตัวใดใช้ tint-black ทั้ง main และ sub ทุกตัวถูกเปิด tint-black
- ไม่ rebuild mesh; ผลมีผลตอน generate ครั้งถัดไป

#### 45. `override void UpdateAnimationState(string name, bool loop, float timeScale)`
- `0x060049b3` · protected · IL 98
- ผู้เรียก: base `SetAnimationState` หลังตั้ง animation บน skeleton หลัก
- ถ้า `m_subSkeletonGraphics.Count <= 0` → return (**ไม่งั้น early-return ทุกครั้ง**)
- วน sub ที่ไม่ null: `sub.AnimationState.SetAnimation(0, name, loop, showErr:false)`;
  `sub.timeScale = 1f`
- **ไม่ใช้ `timeScale` ที่รับมา** — ตั้ง 1 เสมอ
- main skeleton ถูกตั้งโดย base ก่อนแล้ว; hook นี้ทำเฉพาะ sub

#### 46. `override void UpdateAnimationStateWithProgress(string name, float progress, bool loop, float timeScale)`
- `0x060049b4` · protected · IL 130
- ถ้า sub ว่าง → return
- วน sub: `trackEntry = SetAnimation(0,name,loop,showErr:false)`
  - ถ้าไม่ null: `sub.AnimationState.TimeScale = 1f`;
    `sub.Update(trackEntry.AnimationEnd * progress)`;
    `sub.timeScale = timeScale`
- base อัปเดต main ด้วย `AnimationState.Update(...)` และคืน `TimeScale = AnimationSpeed`
  (คนละ property กับ `sub.timeScale`)

#### 47. `override void UpdateAnimationSpeed(float timeScale)`
- `0x060049b5` · protected · IL 82
- ผู้เรียก: base `SetAnimationSpeed` (หลังตั้ง `AnimationState.TimeScale` ของ main)
- ถ้า sub ว่าง → return
- วน sub: `sub.timeScale = 1f`
- **ทิ้งค่า `timeScale`** — sub จะเล่นความเร็ว 1 เสมอ (เจตนา Unknown)

### 2.8 Constructor (48)

#### 48. `.ctor()`
- `0x060049b6` · public · IL 39
- field initializers เท่านั้น: สร้าง `m_subSkeletonGraphics` list และตั้งค่า default
  ของฟิลด์ทั้งหมดใน §1.1 (ไม่มี logic อื่น)
- ค่า default ที่สำคัญ: `m_useClipping = true`, `m_CanvasGroupCompatible = true`,
  `m_PMAVertexColor = true`; `m_RaycastTarget` ไม่ถูกเขียน (คง `false`)

> **การตรวจความครบ 48/48:** ทุกเมธอดใน `ActionSystem_UISpineActor_methods.csv`
> มีหัวข้อของตัวเองในช่วง §2.1–§2.8 — §2.1 มี 8 เมธอด (1–8),
> §2.2 มี 5 (9–13), §2.3 มี 13 (14–26), §2.4 มี 7 (27–33),
> §2.5 มี 7 (34–40), §2.6 มี 3 (41–43), §2.7 มี 4 (44–47), §2.8 มี 1 (48)
> รวม 8+5+13+7+7+3+4+1 = **48**
>
> accessor ทั้ง 6 ตัว (`get`/`set` ของ `s_UISpineAlwaysMult`, `get_skeletonGraphic`,
> `EnablePmaVertexColor`, `EnableCanvasGroupCompatible`, `EnableTintBlack`)
> อยู่ใน §2.1 เป็นหัวข้อเดี่ยวแล้ว ไม่มีการอ้างอิงซ้ำใน §2.8

---

## 3. พฤติกรรมที่สืบทอดจาก `SpineActor` และ UI พึ่งพา

| เมธอด base | บทบาทกับ UI | จุดที่เกี่ยวกับ UI |
|---|---|---|
| `Awake` | `ModelRoot = transform` | `BuildCmpBody`/`SperatorSlot` ใช้ `owner.ModelRoot` |
| `Update` | ถ้า `m_ISkeleton != null`: `UpdateMount()` → slash lerp → `UpdateColor(dt)` | เรียก virtual `UpdateMount`, `FlushSlashProperty`, `DoUpdateColor` |
| `OnEnable/OnDisable/OnDestroy` | register/unregister light (mesh) | UI ไม่มี mesh → no-op โดยปริยาย |
| `LoadBody` | set scale/small, invoke callback เก่า, `CreateRoot`, `CancelAllReq`, `StartLoadMain` | ใช้โดย `UIModelCacheItem` และ `Mount` |
| `GUILoadBody` | set fade-in, priority = `s_UISpineLoadPriority`, `LoadBody(isGUI:true)` | ใช้โดย `UIModelShow.SetTarget` |
| `StartLoadMain` | อ่าน `dbbodyconfig`, set width/height/root scale, `FlushHudOffset()` (UI override), สร้าง `SkinReq`, `Start()` | async AB |
| `OnBodyLoadFinish` | body โหลดเสร็จ → `StartLoadAttachSkins` หรือ fail | |
| `StartLoadAttachSkins` | ถ้าไม่มี attach → `BuildBody`; ไม่งั้น start ทุก req | |
| `OnAttachSkinFinish` | นับจนครบ → `BuildBody` | |
| `BuildBody` | ลำดับ: `CreateSpineComponent` → (overrideSpine → SkeletonUtility) → skin → (`CreateSubSkeleton`) → fade-in → `SetFlipX/Y` scale → `SetAnimationState` → `SetFreezeAnimation` → `DoUpdateColor` → `FlushShadow` → `ReplaySlotEffect` → `StartLoadImageDisplay` → `StartLoadAttachment` → `SetGray` → callback → `SetupSpine` | virtual hooks ถูก override โดย UI |
| `CreateRoot` | `new GameObject("Root")` + `SortingGroup` + `ComputerSortIndex` | UI ปิด SortingGroup ทีหลัง |
| `SetAnimationState` | เลือกชื่อจาก `AnimationNameKey.Get`; `loop` จาก `RepeatCount`/`loopMode`; ตั้ง main `AnimationState.TimeScale`; `SetAnimation`; เรียก virtual `UpdateAnimationState`; ผูก `Complete` ถ้า leave; ส่งต่อ follow actor | UI hook §45 |
| `SetAnimationStateWithProgress` | เหมือนข้างบน + update main ตาม progress; เรียก virtual hook | UI hook §46 |
| `SetAnimationSpeed` | set main `AnimationState.TimeScale`; เรียก virtual `UpdateAnimationSpeed` | UI hook §47 |
| `SetFreezeAnimation` | set `m_FreezeAnimation`; ถ้า `m_SkeletonAnimation` ไม่ null → set `FreezeAnimation` | **UI: no-op** (field null) |
| `StopAnimationState` (base) | set `m_StateID`/`m_AnimationSpeed=0`; ตั้ง main; update; timeScale 0; hook | UI override §16 ไม่ทำส่วน state |
| `SetGray` (base) | เรียก `m_SkeletonAnimation.SetGray` + mount | UI override §22 |
| `RecycleSelf` | `UnMount` → `CleanTweener` → `CancelAllReq` → reset ทุก state → `DestroySkeleton` (virtual) → reset animation → stop effects → `ReleaseFollowActor` → culling | UI override §27 |
| `CancelAllReq` | `ReleaseSkinReq` + image + replace-attachment + `ReleaseFollowActor` | AB refcount |
| `ReleaseFollowActor` | `followActor.RecycleSelf()` + `Destroy(gameObject)` ทุกตัว | UI follow actor ถูกทำลายด้วย |
| `Mount` | `CreateMountActor` → เก็บค่า → `m_MountActor.LoadBody(..., OnMountLoadFinish)` (async) | UI override §36 |
| `OnMountLoadFinish` | `DoUnMount` ถ้า mounting อยู่ → set mounting → activate → flip → `SetUpdateMode` → `DoMount` → callback | UI `SetUpdateMode` ถูกเรียก |
| `DoMount` | ถ้า mount skeleton พร้อม: หา `m_HpBone` จาก `mountHP`; `m_MountHpTransform = mount root`; `m_MountActor.SeparatorSkeleton(...)` | UI separator ทำงานจริงที่นี่ |
| `UnMount` | `mountActor.OnRecycle()`; ถ้า mounting → deactivate | UI เรียก base ตรง ๆ |
| `CreateSubSkeleton` (base) | ว่าง | UI override §41 |
| `CreateSkeletonActor` (base) | สร้าง `SpineActor` (mesh) ใต้ `parent.m_RootTransform` | UI override §28 |
| `BuildCmpBody` (base) | สร้าง mesh, flip, sorting, shader, animation, follow | UI override §29 |
| `CreateFollow` (base) | `BoneFollower` + depth limit 50 | UI override §32 |
| `SeparatorSkeleton` (base) | `SkeletonRenderSeparator.AddToSkeletonRenderer` | UI override §35 |
| `DrawToBuffer` (base) | `commandBuffer.DrawMesh(...)` | UI override §25 ว่าง |
| `SetShaderType` (base) | ZTest/renderQueue ผ่าน material | UI override §26 ว่าง |
| `FlushSlashProperty` (base) | MPB flash | UI override §33 LogError |
| `ChangeSlotDisplay` | `id==0` → `SetAttachment`; `id>0` → `ReplaceAttachmentReq.Start()` | ใช้ `OnAttachmentLoaded` ของ UI |
| `LoadSlot`/`HideSlot`/`AddSkeletonSlot`/`ChangeFollowActorSlotDisplay` | slot/attachment flow | บางเส้นทางถึง UI hooks |
| `PlayEffectOnActor` | เก็บ `SlotEffectInfo` → `PlaySlotEffect` (virtual) | UI §24 |
| `RayHit`/`RayHitPerspective` | `SkeletonBounds` + polygon | UI ใช้ main SG; mount ต่อ |
| `Twinkle` | lerp `m_Slash` → `FlushSlashProperty` | UI error log |

> **Base virtual ที่ base ประกาศว่าง:** `UpdateAnimationState`, `UpdateAnimationStateWithProgress`,
> `UpdateAnimationSpeed`, `CreateSubSkeleton`, `CheckCulling`, `SetAnimation(bool)`, `InitWithActor`
> — ยืนยันว่า design อาศัย subclass เติม hook ไม่ใช่ base ทำทุกอย่าง

---

## 4. Quirks / ข้อควรระวัง (สำคัญต่อความเข้ากันได้)

### 4.1 `raycastTarget` เป็น false เสมอ
`m_RaycastTarget` ถูกอ่านที่ `CreateSkeletonGraphic` แต่ไม่มีการเขียนค่าในคลาส
→ ค่า default `false` ตลอด. ถ้า clone ต้องการให้ภาพรับ raycast ต้องเพิ่ม API แยก (ไม่ใช่พฤติกรรมเดิม)

### 4.2 `StopAnimationState` ไม่บันทึก state
ต่างจาก base: UI ไม่ตั้ง `m_StateID = iState` และไม่ตั้ง `m_AnimationSpeed = 0`
ผลคือ:
- `StateID`/`AnimationSpeed` คงค่าเดิม
- follow actor ไม่ถูก stop ตาม (base จะส่งต่อ `SetFollowActorAnimationState` ใน `SetAnimationState` เท่านั้น)
- sub-skeleton ถูกตั้ง `timeScale = 1` โดย hook → อาจเล่นต่อ
- ถ้า `SetAnimation` คืน null, `AnimationState.TimeScale` ยังเป็น 1

### 4.3 `m_Mat` / `m_GrayMat` ถูกเขียนทับทุกครั้งที่สร้าง SG
`CreateSkeletonGraphic` เป็น method ที่ทั้ง main และ sub เรียก
→ หลัง `CreateSubSkeleton` ค่าสุดท้ายเป็นของ sub ตัวท้าย
→ `SetGray` หลังจากนั้นจะใช้ material ชุดนั้นกับ main + sub ทุกตัว
ถ้า sub มี `tintBlack` ต่างจาก main จะได้ material ผิดสำหรับบางตัว
(เป็น bug ที่สังเกตได้; ไม่มี guard)

### 4.4 PMA ถูกบังคับ false ในเส้นทาง non-tint + canvas-group
ถ้า `!tintBlack && m_CanvasGroupCompatible` → `m_PMAVertexColor = false`
แม้ผู้เรียกจะ `EnablePmaVertexColor(true)` มาก่อน
(ในเส้นทาง tint-black ค่าที่ตั้งไว้ถูกใช้ตามจริง)

### 4.5 `SetFreezeAnimation` ไม่มีผลกับ UI
base เก็บ `m_FreezeAnimation` และพยายาม set `m_SkeletonAnimation.FreezeAnimation`
แต่ UI ไม่มี `m_SkeletonAnimation` → ไม่เกิดอะไร
ถ้า clone ต้องการ freeze ต้องทำที่ `SkeletonGraphic.freeze` หรือ `AnimationState.TimeScale`

### 4.6 `Mount` แยก separator ก่อน mount โหลดเสร็จ
ตาม §36: การเรียก `SeparatorSkeleton` ครั้งแรกมัก fail → fallback sibling
จากนั้น base `DoMount` เรียกซ้ำเมื่อพร้อม → สำเร็จ
ถ้า clone ทำ `Mount` แบบ synchronous จะเห็นลำดับต่างจากเกม

### 4.7 `SeparatorSkeleton` คืน true ถ้ามี separatorSlots อยู่แล้ว
เช็คแค่ `separatorSlots.Count > 0` โดยไม่สนชื่อที่ขอ
→ การเรียกด้วยชื่อชุดใหม่จะไม่เปลี่ยน part จนกว่าจะ clear ก่อน
(ไม่มี API clear ในคลาสนี้; ทำได้ผ่าน `SkeletonGraphic.separatorSlots.Clear()`)

### 4.8 `OnAttachmentLoaded` เรียก `ForceUpdate` ในลูป
และ `ForceUpdate` advance animation 1 frame
→ การโหลด attachment หลาย skin name อาจ advance หลายเฟรม
รวมถึง `Rebuild` หลายครั้ง

### 4.9 `DestroySkeleton` ไม่ clear follower cache
`m_FollowerTransform` ถูกล้างใน base `RecycleSelf` (`m_FollowerTransform.Clear()`)
แต่ถ้ามีการเรียก `DestroySkeleton` ผ่าน `CreateSpineComponent` โดยไม่ recycle
cache จะยังชี้ Transform เก่า (ที่ถูก destroy) → `GetBoneFollowerTransform` คืน Transform ที่ตายแล้ว
(เกิดได้ในเส้นทาง `BuildCmpBody`/rebuild)

### 4.10 ชื่อ/พารามิเตอร์ที่พิมพ์ผิดแต่ต้องคงไว้ถ้าต้องการ drop-in
- `UpdateRectTranformSize` (ไม่ใช่ `Transform`)
- `SperatorSlot` (ไม่ใช่ `SeparatorSlot`)
- `m_SeparatorOrtingOrderIncreament` (base)
- `SetSerparatorSlotNames` (fork ของเกม)
- `SeparaterSlotName` ในพารามิเตอร์ `CreateSkeletonActor`
- `SetMountSeparaterSlotName` (host)

### 4.11 `SetFlipY` ไม่ reset main skeleton scale
ถ้า actor เคย flip ตอนยังไม่มี `m_SepSGRoot` แล้วภายหลังสร้าง sub-skeleton
`m_SkeletonTransform.localScale` อาจค้าง ±1 และถูก parent ใต้ `SepSGRoot` ที่ scale อีกชั้น
→ flip ซ้อน. `SetFlipX` มีการ reset แต่ `SetFlipY` ไม่มี

### 4.12 `CreateFollow` log ผิด bone
ข้อความ error ใช้ `fsActor.attachBoneName` ทั้งที่หา `cmpRootBoneName`
→ ใช้ debug ไม่ได้ตรง

### 4.13 `CreateSkeletonActor` / `PlaySlotEffect` ไม่กัน null
- `GetDBComponentcfg(skinID)` อาจ null → NRE
- `m_SkeletonGraphic.canvas` อาจ null → NRE ใน `PlaySlotEffect`
(ในเกมจริงมี Canvas เสมอสำหรับ UI; แต่ clone ควรกัน)

---

## 5. สรุปขั้นต่ำที่ clone ต้องทำได้เพื่อ "พฤติกรรมเทียบเท่า"

1. สร้าง `SkeletonGraphic` ใต้ `Root` ด้วย material ที่เลือกตาม 4 ชุด + ตั้งค่า
   tintBlack/canvasGroupTintBlack/PMA/allowMultiple/useClipping
2. ทำลาย/สร้างใหม่ได้โดยไม่รั่ว: SG, sub-SG, `SepSGRoot`; คืน separator parts ก่อน destroy
3. ตั้ง animation main ผ่าน base + sync sub-skeleton (แม้จะ "ทิ้ง speed" ตาม quirk)
4. `Stop` แล้ว main หยุด, sub ตาม quirk; `StateID` ไม่เปลี่ยน (ถ้าต้องการ compat เป๊ะ)
5. สลับ material gray/normal; สีผ่าน `SkeletonGraphic.color` (ไม่ premultiply RGB)
6. slot attachment ตรง ๆ ผ่าน `Skeleton.SetAttachment`
7. separator slots + reparent parts ไป parent ของ actor พร้อม sibling index
8. sub-skeleton สำหรับ attach skin + BoneFollowerGraphic (+ จัดลำดับตาม slot index)
9. mount แบบ UI: สร้าง child `SpineMount`, โหลด async, ตำแหน่งตาม bone × PPU,
   unmount = recycle + deactivate (ไม่ destroy)
10. flip ผ่าน `SepSGRoot` เมื่อมี sub-skeleton, ผ่าน skeleton transform เมื่อไม่มี
11. `ForceUpdate` = advance 1 frame + rebuild, ใช้ก่อน cache capture
12. `DrawToBuffer`/`SetShaderType` เป็น no-op; `FlushSlashProperty` แจ้งว่าไม่รองรับ
