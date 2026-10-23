# Spine UI Actor — UIDisciple / UIModelShow / UISpineActor

ขอบเขต: วิเคราะห์แบบอ่านอย่างเดียวจาก `Assembly-CSharp.dll` (ไม่แก้ไข installation)
ข้อความในไฟล์นี้แยกสถานะเป็น **Observed**, **Inferred**, **Unknown** ทุกข้อความในหัวข้อ 1–2 มี Evidence กำกับ

---

## 1. Data-flow summary

แผนที่คำถาม → หัวข้อ: Q1 → 2.2–2.5; Q2 → 1.2–1.3; Q3 → 1.4; Q4 → 1.5; Q5 → 1.6; Q6 → 1.7; Q7 → 1.8; Q8 → 1.9

### 1.1 Evidence key (ใช้ตลอดไฟล์)

| alias | Evidence ID | ครอบคลุม |
|---|---|---|
| E1 | `ev_5a0d910f7ceef3f2848019c606e769189fb589f79f212360a74792bf47315a56` (view `ev_12d420462d2e54634ad8dce27ef788009c3ad0f536e7604ef16f6effc6ed3257`) | member inventory ของ `UIModelShow`, `ActionSystem.UISpineActor`, `InstanceControl` |
| E2 | `ev_1ffe2a3286325f901a6d5bae4c8050bf5d3e9228c526c40ad9747eab38575488` (view `ev_45afd0c2d83b071de2148eaa3dd2b50fbef7f4a0348908f21d923c1c7d6dae49`) | member inventory ของ `SpineActor`, `IActor`, `InstantiateManager` |
| E3 | `ev_cbac4df8830c82bc558ae3361998d7c700e2fa5e08d9c836ba280998af9d0f2d` (parent `ev_5c62ab366503cf56d24aaea921d8b0131915be8ed7b8dadd4ad3879bda0dff41`) | helper types: `SkeletonDataAssetManager`, `AnimationNameKey`, `DragonBonesConfig`, `SkinReq`, `InstantiateManager` ฯลฯ |
| E4 | `ev_928e2b390dfb94009257d034ad8a99818514457f3f4af2594cf8af8927117d42` | artifact identity: MVID `804d1d3b-ea79-4872-9784-771b88e440f4`, SHA-256 `62761be23a4a40518eeb0e98a2d6be6e4b11c3b1bad2064e4c8b528d9876184f` |
| E5 | `ev_be409140e2a72649181eedc5773fb98e7734cc0a3f7bebaf783073eda38648b6` (parent `ev_add4bada4d3ed1d0e504b5d887d22e22df4609a28f94779723e34c7489afe335`) | `Spine.Unity.SkeletonGraphic`, `SkeletonDataAsset`, `SpineAtlasAsset` |

Token ที่ใช้อ้างในตาราง (build-local, ใช้ได้กับ E4 เท่านั้น):
`UIModelShow` = `0x02000059`; `InstanceControl` = `0x02000076`; `UISpineActor` = `0x02000643`;
`SpineActor` = `0x02000640`; fields ของ `SpineActor` = `0x04001f35`–`0x04001f7c` (72 fields);
methods ของ `SpineActor` = `0x060048e3`–`0x06004981` (159 method rows)

`ILSpy:<file>:<line>` = source ที่ decompile จาก assembly เดียวกันกับ E4 (ใช้เป็นหลักฐานเชิงพฤติกรรม ไม่ใช่ token ใหม่)

### 1.2 เส้นทางหลัก (Observed)

```
Lua window / behavior
  └─ uiAIManager:createUIDisciple            (gamesys.home.uiaimanager.txt:71)
     หรือ UIRoleStateManager:createUIDisciple (gamecore.behavior.uirolestatemanager.txt:48)
       └─ _InstantiateManager.AddInstance(INSTANCE_TYPE.eUIDisciple = 1, parent, cb)
          └─ instantiateconfig[1] = { abName="ui/windows/common/instantiate/uidisciple.ab",
                                      assetName="UIDisciple" }
             └─ AssetLoader.LoadAssetAsGameObject → Instantiate(prefab)
                + gameObject.AddComponent<InstanceControl>().guid = guid
                   └─ prefab UIDisciple: UIModelShow + CSGUIWidgetBase + CanvasGroup
                      ├─ child[0] = root (UIModelShow host)
                      ├─ child[1] = "hudtarget"
                      └─ child[2] = "click" (Button)
       └─ setDiscipleModel / createDiscipleModel
          └─ UIDiscipleModel:getDiscipleOutsideModelInfoByData / getDiscipleInsideModelInfoByData / getImageInfoHeadModelInfo
             └─ dzWidget:SetChildUIModelShowTarget(0, body, dzScale*scale, componets, eAnimationID.stand)
                └─ CSGUIWidgetBase.SetChildUIModelShowTarget (:3184)
                   └─ UIModelShow.SetTarget(body, scale, cmps, anim, stopAnim, softMask, fadeIn, cb)
                      └─ DragonBonesConfig.IsSpineBody(body)
                         ├─ true  → new GameObject("SpineActor") + UISpineActor
                         └─ false → new GameObject("DragonActor") + DragonBonesActor
                      └─ UISpineActor.GUILoadBody → SpineActor.LoadBody → StartLoadMain
                         └─ dbbodyconfig[body] → SkinReq(spineAb, spineSkins)
                            └─ SkeletonDataAssetManager.LoadData → AssetLoader.LoadAsset<SkeletonDataAsset>
                               └─ AssetBundleCacheManager.SearchAssetbundleURL
                         └─ BuildBody → CreateSpineComponent → UISpineActor.CreateSkeletonGraphic
                            └─ Skin aggregation (SetSkin / AddSkin / SetAttachment)
                               └─ SkeletonGraphic.EnsureCanvasRendererCount → child "Renderer0..n"
                      └─ OnLoadBodyFinish → FlushHUDTarget + InitFollow + GenRenderTexure
```

หลักฐานของแต่ละช่วง:

1. **Lua สร้าง instance** — `uiAIManager:createUIDisciple` เรียก `_InstantiateManager.AddInstance(otherData.instanceType or INSTANCE_TYPE.eUIDisciple, parent, cb)` (Observed, `gamesys.home.uiaimanager.txt:71-76`); `UIRoleStateManager:createUIDisciple` เรียกค่าเดียวกันแบบ fix (`gamecore.behavior.uirolestatemanager.txt:48-51`).
2. **`eUIDisciple = 1`** — `INSTANCE_TYPE.eUIDisciple` ถูกใช้เป็น id ของ `instantiateconfig`; แถว id `1` ชี้ `abName="ui/windows/common/instantiate/uidisciple.ab"`, `assetName="UIDisciple"` (Observed, parse `rawdata.instantiateconfig` → `extracted/instantiateconfig.json`, 177 records; prefab decode ด้วย UnityPy).
3. **การโหลด prefab** — `InstantiateManager.AddInstance` อ่าน `m_Config.Get(type)` แล้วเรียก `AssetLoader.LoadAssetAsGameObject(null, cfg.abName, cfg.assetName, ...)` จากนั้น `Object.Instantiate(sp, parent)` และ `AddComponent<InstanceControl>().guid = guid` (Observed, `InstantiateManager.cs:52-84`; type token `0x02000076`, E1).
4. **Prefab composition** — root `UIDisciple` 100×100 มี `UIModelShow` (`m_Script` fileID `8702132792036946060`) และ `CSGUIWidgetBase` (`3982155713779490877`) + `CanvasGroup`; ลูกชื่อ `hudtarget` (index 1, 100×100) และ `click` (index 2, 100×135, `Button` fileID `-787143586316466668`, `CSGUIRect`, `CanvasRenderer`) (Observed, UnityPy บน `uidisciple.ab`; artifact identity E4).
5. **ค่าที่ serialize ใน prefab** — `HudTarget` → fileID `7250475308219960788`; `FollowPairs=[]`; `DefaultOrthographicSize=260`; `DefaultNearPlane=-1`; `DefualtFarPlane=1`; `CanvasGroupCompatible=1`; `PMAVertexColor=1`; `TintBlack=0`; `FadeoutTime=0.6`; `RenderTextureScale=2`; `modelId=0`; `scale=1`; `animId=0`; `ChildTransformArray=[self, hudtarget, click]` (Observed, UnityPy).
6. **Lua เลือกโมเดลและเรียก `UIModelShow`** — `setDiscipleModel` เรียก `SetChildUIModelShowTarget(0, modelParams.body, dzScale*scale, modelParams.componets, eAnimationID.stand)` (Observed, `gamesys.home.uiaimanager.txt:162`; `gamecore.behavior.uirolestatemanager.txt:111`).
7. **`CSGUIWidgetBase` ส่งต่อไปยัง `UIModelShow`** — `SetChildUIModelShowTarget` = `GetChildTransform(index).GetComponent<UIModelShow>().SetTarget(modelID, size, componnets, animationID, stopAnim, softMask, fadeIn, action)` (Observed, `CSGUIWidgetBase.cs:3184-3187`).
8. **`SetTarget` สร้าง actor** — `CreateActor(modelID)` เช็ค `Singleton<DragonBonesConfig>.Instance().IsSpineBody(modelID)`; ถ้าจริงสร้าง `new GameObject("SpineActor")` + `AddComponent<UISpineActor>()`, ไม่งั้นสร้าง `DragonActor` + `DragonBonesActor` (Observed, `UIModelShow.cs:213-232`; `DragonBonesConfig.IsSpineBody` = มี `dbbodyconfig.spineAb` ไม่ว่าง, `DragonBonesConfig.cs:34-42`).
9. **`IsSpineBody` → `UISpineActor`** — `UISpineActor : SpineActor` (token `0x02000643`, base `0x02000640`) (Observed, E1; `UISpineActor.cs:19`).
10. **โหลด body** — `UIModelShow.SetTarget` เรียก `m_ActorTarget.GUILoadBody(modelID, componnets, scale, softMask, fadeIn, cb)` หลังตั้ง animation (Observed, `UIModelShow.cs:246-271`); `GUILoadBody` ตั้ง priority แล้วเรียก `LoadBody(..., isGUI:true, ...)` (`SpineActor.cs:736-741`).
11. **เลือก bundle** — `StartLoadMain` ตั้ง `assetAb = (m_IsSmall && !string.IsNullOrEmpty(m_BodyCfg.s_spineAb)) ? m_BodyCfg.s_spineAb : m_BodyCfg.spineAb` แล้วสร้าง `new SkinReq(mainSkinID, m_BodyCfg.spineSkins, assetAb, ...)` (Observed, `SpineActor.cs:240-258`).
12. **โหลด asset** — `SkinReq.Start()` → `SkeletonDataAssetManager.LoadData(this)`; `LoadAsset` เรียก `AssetLoader.LoadAsset<SkeletonDataAsset>(new object[]{ req.AssetAbName }, req.AssetAbName, null, syncLoad:false, 0, OnAssetLoaded, null)` (Observed, `SkinReq.cs:53-57`; `SkeletonDataAssetManager.cs:154-158`).
13. **สร้าง SkeletonGraphic** — `BuildBody` → `CreateSpineComponent(m_BodySkinReq.Data.skeletonDataAsset)` → `UISpineActor.CreateSkeletonGraphic(...)` (Observed, `SpineActor.cs:490-499`; `UISpineActor.cs:119-171`).
14. **รวม skin** — ถ้าไม่มี attach component: `CreateSkeletonSkinWithNoCmp` (1 skin → `data.FindSkin` + `skeleton.SetSkin`; หลาย skin → `new Skin("base_skin")` + `skin2.AddSkin(skin3)` + `SetSkin`) (Observed, `SpineActor.cs:457-488`). ถ้ามี component: สร้าง `new Skin("base_skin")`, `AddSkin(...)` แล้ว `m_ISkeleton.Skeleton.SetSkin(skin)` (Observed, `SpineActor.cs:490-556`).
15. **เสร็จแล้วจัด HUD/таймер** — callback ของ `GUILoadBody` → `OnLoadBodyFinish()` → `FlushHUDTarget(); InitFollow(); GenRenderTexure();` (Observed, `UIModelShow.cs:234-244`).

### 1.3 ใครเรียก `UIModelShow` ด้วยข้อมูลอะไร

| ผู้เรียก | ข้อมูลที่ส่ง | หลักฐาน |
|---|---|---|
| `uiAIManager:setDiscipleModel` (world/AI) | `body = getDiscipleOutsideModelInfoByData(info).body`; `size = isometricMapSystem:getModelScale(body,true) * scale`; `componnets = face/hair out_side + weapon out_side`; `anim = eAnimationID.stand (0)`; `stopAnim=false` | Observed, `gamesys.home.uiaimanager.txt:123-166` |
| `UIRoleStateManager:createDiscipleModel` | เหมือนข้างบน แต่ scale มาจากพารามิเตอร์ + `isometricMapSystem:getModelScale` | Observed, `gamecore.behavior.uirolestatemanager.txt:82-115` |
| `comHelper.setChildInSideModel*` (half-body ใน UI) | `body = getDiscipleInsideModelInfo(...).body`; `size = scale or modelParams.scale`; `componnets = in_side + accessory`; `anim = anim or 0`; `stopAnim = false` โดย default | Observed, `common.utils.comhelper.txt:36-70` |
| `comHelper.setChildHead*` (head icon) | `body = getImageInfoHeadModelInfo(...).body (out_side)`; `componnets = face/hair out_side`; `anim = 0`; `stopAnim = true` โดย default; ตามด้วย `SetChildUIModelShowTargetOffset(headOffset[1], headOffset[2])` | Observed, `common.utils.comhelper.txt:9-34`; `gui.windows.disciple.uidisciplemodel_model.txt:290-330` |
| หน้าต่างทั่วไป (`uibottommask*`, `uicommonpagewin`, list/tab) | body id ตรง ๆ (2017, 3037, 2069 ฯลฯ) + `eAnimationID.common_window_enter/stand/bd_stand` | Observed, grep `eAnimationID.common_window_*` / `bd_stand` ใน `ab_text/binary` |

นอกจาก 2 wrapper ข้างต้น ยังพบ **11 call site** ที่เรียก `_InstantiateManager.AddInstance(INSTANCE_TYPE.eUIDisciple, ...)` ตรง ๆ (ไม่ผ่าน `createUIDisciple`):
`uiaimanager:248` (`createALingShou`), `uiaimanager:322` (`createNPC`), `uisubact_fuzhixinlingwin:180`, `uisubact_qiandaowin:127`, `uisubact_qiandaowin_kpbl:136`, `uidlroomwin:445`, `uidlcultivationwin:549`, `uililianwin:351`, `uidzroomwin:1073`, `uixbsl_mainwin:617`, `uizhenfastudywin:452` (Observed, grep `INSTANCE_TYPE.eUIDisciple` ใน `ab_text/binary`).
ยังมีอีกกลุ่มที่ส่ง `INSTANCE_TYPE.eUIDisciple` ผ่าน `uiAIManager:createUIObject`/`createEmptyObject` (เช่น `uibinggongchangwin:492`, `uicatshopwin:165`, `uishanghangwin:311`, `uiyulingzhaiwin:755/785`) — กลุ่มนี้เรียก `AddInstance(stType, ...)` เองแล้ว `SetChildUIModelShowTarget(0, bodyId, ...)` ด้วย body id ที่ส่งเข้ามา (`gamesys.home.uiaimanager.txt:180-190`) จึงใช้ prefab `UIDisciple` เดียวกันแต่ไม่ผ่าน `setDiscipleModel`.

### 1.4 Q3 — full body vs chibi (Observed + Inferred)

**Observed — โมเดลเต็มตัว (`out_side`)**
- `getDiscipleOutsideModelInfoByData` เริ่มจาก `bodyCfg = cfg_disciplebodyimageconfig_get[imageInfo.body]`, เลือก `result.body = bodyCfg.out_side` (หรือ `out_tm[tmLv]`, `xm_transfer[xianmo_voc]`, `clothing_map`/`clothing_out_side`) และ `result.componets` จาก face/hair `out_side` (`gui.windows.disciple.uidisciplemodel_model.txt:366-484`).
- `getImageInfoHeadModelInfo` ก็ใช้ `bodyCfg.out_side` + `headOffset` สำหรับ head icon (`:295-330`).
- ตัวอย่างจริง body image `1001`: `out_side=204021` → `spineAb="character/spinelib/body_female/body_1120402_skeletondata.ab"`, `spineSkins=["body_1120402_1"]` (Observed, `dbbodyconfig.json` + `disciplebodyimage.json`).

**Observed — ครึ่งตัว/จิบิใน UI (`skeletonID`, `in_side`)**
- `getDiscipleInsideModelInfoByData` เลือก `result.body = bodyCfg.skeletonID` (หรือ `skeletonID_bg` เมื่อ `args.bgFisrt`) และ `result.componets` จาก `in_side`/`in_side_bg` + hair/face/accessory `in_side` (`:84-188`).
- ตัวอย่าง body image `1001`: `skeletonID=100012` → `character/spinelib/halfbody_dizi_female/huashi/2120603_skeletondata.ab` (`spineSkins=[]`); `in_side={704322}` → `halfbody_dizi_female/wuji/body_2120408_4_1_...` (Observed, `dbbodyconfig.json` + `disciplebodyimage.json`).
- ทั้งชุด `halfbody_dizi*` อยู่ใต้ `character/spinelib/halfbody_dizi*` ส่วน full body อยู่ใต้ `character/spinelib/body*` / `special_dizi` (Observed, ชื่อ path ใน `dbbodyconfig.spineAb`).

**Inferred**
- ไม่มี flag ชื่อ `isChibi` ใน C# หรือ Lua (Observed: ไม่พบ `isChibi`/`IsChibi`/`chibi` ใน ILSpy corpus และ `ab_text`); "จิบิ" ในโปรเจกต์นี้ = โมเดลที่มาจาก `skeletonID`/`skeletonID_bg` (halfbody) ไม่ใช่ `out_side` (full body) — เป็นข้อสรุปจาก path/config ไม่ใช่จากการประกาศ type (E1/E2/E3 + `dbbodyconfig.json`).
- ตัวกำหนดว่าได้ full body หรือ half body คือ **หน้าต่าง/ฟังก์ชัน Lua ที่เรียก** (`getDiscipleOutside*` vs `getDiscipleInside*`) + ค่าใน `disciplebodyimageconfig` ของ body image นั้น ไม่ใช่ค่าที่ส่งเข้ามาใน `UIModelShow`.

### 1.5 Q4 — appearance data model → Spine API

**Observed — data model**
- `netData.discipledata` เป็น int32: color 4 bits / race 8 / sex 4 / job 16; `discipleimage` เป็น int64: hair 16 / face 10 / body 16 / accessory 10 (`gui.windows.disciple.uidisciplemodel_tool.txt:26-45`); เพิ่ม `clothingId`, `clothingStar`, `xianmo_voc`, `hidedress`, `hidexianmodress`.
- Lua แปลง `(body, componets[], scale, anim)` เป็น id ที่ C# โหลด; C# ไม่เห็น hair/face โดยตรง เห็นเพียง `modelID` + `int[] componnets` (Observed, `UIModelShow.SetTarget` signature; `SpineActor.StartLoadMain`).

**Observed — mapping ไปยัง Spine API**

| ข้อมูล | เส้นทาง | Spine API |
|---|---|---|
| body skins | `dbbodyconfig[body].spineSkins` → `SkinReq.SkinNames` | `SkeletonData.FindSkin(name)` + `Skeleton.SetSkin(skin)` เมื่อมี skin เดียว; หลาย skin → `new Skin("base_skin")` + `Skin.AddSkin` + `SetSkin` (`SpineActor.cs:457-488`) |
| component skins (ไม่ attach) | `dbcomponentcfg[id].spineSkins` → `AddAttachmentSkinReq` → `SkinReq` | `AddSkin(newSkin, attachSkin, slotMap)` คัดลอก bones/constraints/attachments ตาม **slot index** (`newSkin.SetAttachment(value.Data.Index, value.Data.Name, attachment)`) (`SpineActor.cs:412-436`) |
| component ที่ attach กับ slot | `followSlot` + `cmpRootBone` | `LoadSkeletonAttachment` รวม attachment เข้า slot เดิม (`SpineActor.cs:531-536`) |
| component ที่เป็น skeleton แยก | `followSlot` อย่างเดียว | `CreateSubSkeleton` → `CreateSubSkeletonGraphic` (SkeletonGraphic ลูก) (`UISpineActor.cs:664-715, 761-793`) |
| weapon/ชิ้นส่วนราย slot | `SetChildLoadSlot(0, slotName, outSide)` → `LoadSlot` → `ChangeSlotDisplay` | `ReplaceAttachmentReq(targetSkinID)` → โหลด `dbcomponentcfg` → `Skeleton.SetAttachment(slotName, attachmentName)` (`SpineActor.cs:1070-1075, 1816-1856`) |
| direct slot override | `UIModelShow.SetSlotAttachment` | `UISpineActor.SetSlotAttachment` → `m_SkeletonGraphic.Skeleton.SetAttachment(slotName, attachName)` (`UISpineActor.cs:323-329`) |

กรณี `spineSkins` ว่าง (เช่น half-body `100012`) → `CreateSkeletonSkinWithNoCmp` เข้า branch `num == 0` → `skeleton.SetToSetupPose()` (`SpineActor.cs:484-487`)

**Observed — สี**
- ไม่พบเส้นทาง tint ราย slot แบบ RGB: `SpineActor.SetSlotInheritColor` ตั้ง فقط `slot.A = 0/1` (`SpineActor.cs:1279-1297`).
- สีทั้งตัวมาจาก `SkeletonGraphic.color` ใน `UISpineActor.UpdateColor_Imp` (main + sub graphics) (`UISpineActor.cs:365-380`); `DoUpdateColor` คูณ alpha ด้วย `m_FadeInAlpha` (`:354-363`).
- `imageInfo.color` ถูกใช้เป็นสีพื้นหลังไอคอนหัว (`image_gwtouxiangpjk_{color}`) ไม่ใช่สี Spine (Observed, `comHelper.setChildModelHeadIconBGByColor` ใน `common.utils.comhelper.txt`).

### 1.6 Q5 — multiple Renderers

**Observed (กลไก)**
- `SkeletonGraphic.EnsureCanvasRendererCount` สร้าง child `Renderer{i}` (RectTransform + CanvasRenderer + RawImage) ตามจำนวนที่ต้องการ (`spine-unity/SkeletonGraphic.cs:785-799`).
- `UpdateMeshMultipleCanvasRenderers` สร้าง mesh หนึ่งชุดต่อ `submeshInstructions` หนึ่งรายการ (`:706-783`).
- `MeshGenerator` แยก submesh เมื่อ material เปลี่ยน หรือเมื่อ `forceSeparate` (`MeshGenerator.cs:334`); separator slot ตั้ง `forceSeparate` (`:288-299`).
- `UISpineActor.CreateSkeletonGraphic` เปิด multi-renderer เมื่อ `s_UISpineAlwaysMult || atlasAssets.Length > 1 || (atlasAssets.Length == 1 && atlasAssets[0].MaterialCount > 1)` (`UISpineActor.cs:162`); sub-skeleton ตั้ง `allowMultipleCanvasRenderers = true` เสมอ (`:767`); separator slots สร้าง `SeparatorParts` ชื่อ `{skinID}_{j}_{slotIndex}` (`:838-849`).

**Observed (ข้อจำกัดของหลักฐาน)**
- Full-body ตัวอย่าง `special_dizi/2113606_full_skeletondata.ab` (882,935 bytes) มี **1 atlas asset / 1 material** (`2113606_full_Material`) (Observed, UnityPy).
- สแกน `*_skeletondata.ab` ที่ติดตั้งใน `StreamingAssets` 30 ไฟล์: ทุกไฟล์มี 1 atlas; มากสุด 2 materials (`dengluye2`, `dengluye3`); ไม่มีไฟล์ไหนมี 5 materials (Observed, UnityPy).

**Inferred — ทำไมโมเดลเต็มตัวมี 5 Renderer**
1. **หลาย SkeletonGraphic จาก component ที่มี `followSlot`** — body 1 + sub-skeleton 4 = 5 `Renderer` ทั้ง subtree (แต่ละ SkeletonGraphic สร้าง `Renderer0` ของตัวเอง) — เป็นคำอธิบายที่เข้ากับโค้ด `CreateSubSkeleton` มากที่สุด.
2. **5 submesh segments ใน SkeletonGraphic เดียว** — เกิดจาก material เปลี่ยนระหว่าง slot (blend mode / `forceSeparate`) ไม่ใช่จำนวน atlas page; `MaterialCount` นับเฉพาะ material ต่อ page จึงไม่ฟันธงจาก 1 material ว่าได้ 1 renderer.
3. **main body ถูกแบ่งเป็น 5 separator parts** — เมื่อมี component แบบ `followSlot` อย่างเดียวอย่างน้อย 1 ตัว `CreateSubSkeleton` จะเรียก `SeparatorSkeletoGraphics(m_BodySkinReq, m_SkeletonGraphic, ...)` ซึ่งแบ่ง main graph ตาม **ช่วง slot index ที่ติดกัน** ของ slot ที่มี attachment (`value2 - num != 1` = จุดตัด) แล้วตั้ง `allowMultipleCanvasRenderers`/`enableSeparatorSlots`/`SetSerparatorSlotNames`; ชื่อ parts = `{bodySkinID}_{j}_{slotIndex}` (`UISpineActor.cs:811-849`) — ไม่ต้องมี material หลายหน้า.

**Observed — กติกาการแบ่ง main body**
`SeparatorSkeletoGraphics` return ทันทีถ้า `target.Skeleton.Skin.Attachments.Count <= 0`; ถ้า `list.Count == 1` จะไม่แบ่ง (แค่เปลี่ยนชื่อ object เป็น `{skinID}_0_{index}`); ถ้า `>1` จึงแบ่งเป็น `SeparatorParts` จำนวน `list.Count` (`UISpineActor.cs:794-855`).

**Inferred (ต่อ)**
4. ถ้า Renderer 5 ตัวเป็นของ main graph เดียว ตัวเลข 5 = 4 จุดตัดของ slot index + 1 (ไม่เกี่ยวกับจำนวน atlas page) — ต้องยืนยันด้วยชื่อ `{bodySkinID}_{j}_{slotIndex}` และ `SeparatorParts.Count`.

**Unknown**
- ไม่ทราบว่า full-body ตัวที่ผู้ใช้เห็น 5 Renderer คือ body id ใด และ 5 นั้นนับเฉพาะลูกของ `SkeletonGraphic` หลักหรือนับรวม sub-skeleton ทั้งหมด.
- ค่า runtime ของ `UISpineActor.s_UISpineAlwaysMult` (เป็น `[AppDataModelOption]` static property, `:57`) ไม่ทราบว่าถูกเปิดใน build นี้หรือไม่.

**ข้อบังคับเชิงตรรกะ (Observed + Inferred)**
`SkeletonGraphic.UpdateMesh` เลือก `UpdateMeshSingleCanvasRenderer()` เมื่อ `allowMultipleCanvasRenderers == false` และเลือก `UpdateMeshMultipleCanvasRenderers()` (ซึ่งเรียก `EnsureCanvasRendererCount`) เมื่อเป็น `true` (`SkeletonGraphic.cs:603-616`). ดังนั้นถ้า body มี 1 atlas/1 material และไม่มี separator/sub-skeleton **และ** `s_UISpineAlwaysMult == false` จะได้ `Renderer` เดียว — เคส 5 Renderer จึงต้องเข้าเงื่อนไขใดเงื่อนไขหนึ่งใน 3 ข้อข้างต้น.

### 1.7 Q6 — animation/state ต่อหน้า

**Observed — id → state**
`AnimationNameKey.Get(id)` คืน `dbanimationstate` จาก `RawDataIntDictionary` (`AnimationNameKey.cs:7-14`); `SetAnimationState` ใช้ `RepeatCount == -1` เป็น loop, ถ้า `LeaveTo != -1` ผูก `Complete` ไป state นั้น (`SpineActor.cs:914-950`); `StopAnimationState` เล่นครั้งเดียวแล้ว freeze ที่ `AnimationEnd * progress` (`:994-1022`). `BuildBody` เรียก `SetAnimationState(m_StateID, m_AnimationSpeed)` ซ้ำตอนท้าย (`:576`) ทำให้ state ที่ `UIModelShow` ตั้งไว้ก่อนโหลดถูก apply จริงเมื่อ skeleton พร้อม.

| id | `dbanimationstate.Name` | loop / leaveTo | ใช้ที่ |
|---|---|---|---|
| `stand=0`, `idle=2` | `stand` | repeat `-1` (loop), leaveTo `-1` | world/AI (`uiAIManager:createUIDisciple`, `createNPC`, `createALingShou`), `UIRoleStateManager`, inside model (`setChildInSideModel`, `stopAnim=false`) |
| `enter=5` | `enter` | repeat `1`, leaveTo `0` → `stand` | เปิดหน้าตัวละคร/daoyan (ชุด `{5,3460,3461}`) |
| `enter2=3460` | `enter2` | repeat `1`, leaveTo `12` (`stand2`) | `uidiscipledaoyanwin`, `uidiscipledaoyanupwin` |
| `enter3=3461` | `enter3` | repeat `1`, leaveTo `13` (`stand3`) | `uidiscipledaoyanunlockwin:100`, `uidiscipledaoyanlocktipswin:106` |
| `common_window_enter=2040` | `enter` | repeat `1`, leaveTo `2041` | เปิด window frame (`uibottommask*`, `uicommonpagewin`, list/tab) |
| `common_window_stand=2041` | `stand` | repeat `-1` (loop), leaveTo `-1` | `uitablistcomponent*`, `uidzroomwin:886` |
| `common_window_dianji=2042` | `dianji` | repeat `1`, leaveTo `2041` | ปุ่ม/ไอเท็มใน list (`SetChildModelAnimationState(2, ...)`) |
| `bd_stand=100` | `stand` | repeat `1`, leaveTo `-1` (เล่นครั้งเดียว) | background body 3037 (`uicommonpagewin:140`, `uidanfangselectwin:175`, `uidisciplejingjiewin:2674`) |
| `juanzhou_idle1=2017` | `stand1` | repeat `-1` (loop), leaveTo `-1` | โมเดลไอเท็มใน tab/list ก่อนเปลี่ยนเป็น `dianji` (`uitablistcomponent.txt:97-99`) |

**Observed — head icon**
`setChildHead` ส่ง `stopAnim=true` เป็น default → `UIModelShow.SetTarget` เรียก `StopAnimationState(animationID)` **หลัง** โหลด body เสร็จ (`UIModelShow.cs:262-269`) → ได้ภาพนิ่งที่ progress 0.

### 1.8 Q7 — `hudtarget` และ `click`

**`hudtarget` (Observed)**
- เป็น child index 1 ของ prefab และถูก serialize เข้า `UIModelShow.HudTarget` (UnityPy; `UIModelShow.cs:80`).
- `FlushHUDTarget()` ตั้ง `HudTarget.localPosition = m_ActorTarget.GetHudOffset()` และกลับเครื่องหมาย X เมื่อ `GetFlipX()` (`UIModelShow.cs:299-310`).
- `SpineActor.FlushHudOffsetImp(scaleFactor)` คำนวณ `m_HudOffset = dbbodyconfig.headPos * m_Scale * scaleFactor` (`SpineActor.cs:1455-1472`); `UISpineActor` override ด้วย `125f` (`UISpineActor.cs:349-352`) ส่วน world actor ใช้ `1f`. ตัวคูณ 125 นี้สอดคล้องกับ Lua ที่คำนวณ `headPos[1]*scale*125` เองใน `createALingShou` (`gamesys.home.uiaimanager.txt:263-266`).
- **ผู้ใช้ค่า**: `UIRoleSpeakNode` อ่าน shared var `target` (behavior ใช้ `target="@hudtarget"`) แล้ว `widget:GetCommonComponent(wIndex,'Transform')` เป็น parent ของ prefab `eUIDiscipleSpeak` (`gamecore.behavior.actions.ui.uirolespeaknode.txt:53-74`); `eUIDiscipleSpeak` = `instantiateconfig` id `2` (`UIDiscipleSpeakHUD`, bundle `uidisciplespeakhud.ab`) ตาม `instantiateconfig.json`; หน้าต่าง lingshou ตั้ง `hudtarget=1` ใน mapping child index (`gui.windows.lingshou.uilingshouysffanyanwin.txt:262-272`).
- สรุป: `hudtarget` = **จุดอ้างอิงเหนือหัวตัวละครสำหรับ speech bubble/HUD** ไม่ใช่ bone ของ Spine และไม่ได้ถูกอ่านโดย `UIModelShow` เองเลย.

**`click` (Observed)**
- เป็น child index 2 ที่มี `Button`; `SetChildButtonClick(index, ...)` เลือก `GetChildTransform(index).GetComponent<Button>()` แล้ว `onClick.RemoveAllListeners()` + `AddListener` (`CSGUIWidgetBase.cs:1959-1975`).
- `uiAIManager` ผูก handler **เฉพาะ** state `DISCIPLE_STATE_TYPE.eChuiWei` → `discipleSelectController.onJiuZhiClick(dzId)`; state อื่นเรียก `SetChildButtonClick(2, nil, true, 0)` เพื่อล้าง listener (`gamesys.home.uiaimanager.txt:145-161`; `gamecore.behavior.uirolestatemanager.txt:100-110`).
- `m_OnClick` ใน prefab ว่าง (UnityPy) → ไม่มี handler ติดมากับ prefab; Lua เป็นผู้ผูกทั้งหมด. ตัวอย่างการปิดปุ่ม: `createNPC` เรียก `SetChildActive(2, false)` (`gamesys.home.uiaimanager.txt:331`).

### 1.9 Q8 — asset loading และที่มาของ key

| ระดับ | key | ที่มา | หลักฐาน |
|---|---|---|---|
| prefab | `instantiateconfig[1].abName` = `ui/windows/common/instantiate/uidisciple.ab`, `assetName` = `UIDisciple` | `rawdata.instantiateconfig` | Observed, `InstantiateManager.cs:52-64`; `instantiateconfig.json` |
| body skeleton | `dbbodyconfig[body].spineAb` (หรือ `s_spineAb` เมื่อ `m_IsSmall`) | `rawdata.dbbodyconfig` | Observed, `SpineActor.cs:257-258`; `dbbodyconfig.json` (4,574 rows) |
| body skins | `dbbodyconfig[body].spineSkins` | เดียวกัน | Observed, `SpineActor.cs:258` |
| component | `dbcomponentcfg[id].spineAb` + `spineSkins` + `followSlot` + `cmpRootBone` | `rawdata.dbcomponentcfg` | Observed, `SpineActor.cs:290-298`; `dbcomponentcfg.json` (6,174 rows) |
| slot/weapon | `cfg_discipleweaponimageconfig[weaponId].out_side` → `LoadSlot` → `dbcomponentcfg[outSide]` | `data.ab` Lua config | Observed, `gamesys.home.uiaimanager.txt:163-166`; `SpineActor.cs:1816-1856` |
| animation | `dbanimationstate[id].Name` | `rawdata.dbanimationstate` | Observed, `AnimationNameKey.cs:11-14`; `dbanimationstate.json` (513 rows) |
| cache | `SkeletonDataAssetManager.m_SkeletonDataAssets` / `m_ReqLoadDatas` keyed by `AssetAbName` | runtime | Observed, `SkeletonDataAssetManager.cs:12-18, 90-158` |
| path | `WritableFilePath → StreamingPath → UpdateManager download URL` | `AssetBundleCacheManager.SearchAssetbundleURL` | Observed, `AssetBundleCacheManager.cs:225-287` |

**Observed — `_s` variant**
`s_spineAb` ถูกเลือกเฉพาะเมื่อ `m_IsSmall && s_spineAb` ไม่ว่าง (`SpineActor.cs:257`, `:297`); `LoadBody` มี default `small=false` (`:713`) และ `GUILoadBody` ไม่ส่ง `small` (`:736-741`) → เส้นทาง `UIModelShow`/`UIDisciple` ใช้ `spineAb` ปกติ.

---

## 2. Type table

### 2.1 ตารางสรุปชนิดข้อมูล

`role` = หน้าที่ในระบบ; `key field/method` = สมาชิกที่อธิบายพฤติกรรม; `evidence` = token/Evidence ID

ทุกแถวในตารางนี้เป็น **Observed** (ชนิด/สมาชิกมีจริงใน metadata ของ E1/E2/E3/E5 หรือตรวจได้จาก artifact E4) — ข้อความเชิงอนุมาน/ไม่ทราบถูกแยกไว้ในหัวข้อ 1.4–1.6 และหัวข้อ 3 เท่านั้น

| name | role | key field/method | evidence (token/Evidence ID) |
|---|---|---|---|
| `UIModelShow` | host component บน prefab `UIDisciple`; สร้าง/ทำลาย actor, ส่ง animation/color/slot, จัด `HudTarget` | `SetTarget`, `CreateActor`, `FlushHUDTarget`, `OnLoadBodyFinish`, `SetAnimationState`, `StopAnimationState`, `SetSlotAttachment`, `LoadSlot`, `HudTarget`, `modelId`, `scale`, `animId` | E1; token `0x02000059`; `UIModelShow.cs:246-271, 213-232, 299-310` |
| `UIModelShow.FollowPair` | ผูก transform กับ slot ของ DragonBones actor (ไม่ใช่ Spine) | `targetName`, `follow`, `offset`, `followRotation`, `Init(IActor)`, `Update()` | E1; `UIModelShow.cs:15-65` |
| `ActionSystem.UISpineActor` | UI actor ที่ใช้ `SkeletonGraphic`; override การสร้าง/สี/แยก slot | `CreateSkeletonGraphic`, `SetSlotAttachment`, `UpdateColor_Imp`, `CreateSubSkeleton`, `SeparatorSkeletoGraphics`, `FlushHudOffset`, `s_UISpineAlwaysMult` | E1; token `0x02000643` (base `0x02000640`); `UISpineActor.cs:119-171, 323-380, 664-855` |
| `ActionSystem.UISpineActor.IndexTransform` | คู่ (slot index, Transform) สำหรับ map separator/sub-skeleton | `t`, `index`, ctor | E1; `UISpineActor.cs:21-32` |
| `InstanceControl` | marker บน instance ที่ `InstantiateManager` สร้าง; คืน guid ตอน destroy | `guid`, `OnDestroy` | E1; token `0x02000076`; `InstanceControl.cs:3-11` |
| `ActionSystem.SpineActor` | ฐานของ `UISpineActor`; โหลด body/component, ประกอบ skin, animation state, HUD offset, mount | `StartLoadMain`, `AddAttachmentSkinReq`, `BuildBody`, `AddSkin`, `CreateSkeletonSkinWithNoCmp`, `SetAnimationState`, `StopAnimationState`, `SetSlotInheritColor`, `FlushHudOffsetImp`, `GetHudOffset` | E2; token `0x02000640`; fields `0x04001f35`–`0x04001f7c`; methods `0x060048e3`–`0x06004981` |
| `ActorSystem.IActor` | interface กลางของ actor (DragonBones/Spine) ที่ `UIModelShow` ใช้ | `GUILoadBody`, `SetAnimationState`, `StopAnimationState`, `GetHudOffset`, `GetFlipX`, `LoadSlot`, `HideSlot`, `ChangeSlotDisplay`, `AddSkeletonSlot` | E2; `IActor.cs:32-96`; E1 (`UIModelShow.m_ActorTarget`) |
| `InstantiateManager` | static loader ของ prefab ตาม `instantiateconfig` + guid lifecycle | `AddInstance`, `GetComponent`, `RemoveInstance`, `m_Config` | E2/E3; `InstantiateManager.cs:29-89` |
| `SkeletonDataAssetManager` | cache/loader ของ `SkeletonDataAsset` ต่อ `AssetAbName` | `LoadData`, `LoadAsset`, `m_SkeletonDataAssets`, `m_ReqLoadDatas` | E3; `SkeletonDataAssetManager.cs:12-18, 90-158` |
| `SpineSurport.SkinReq` | request หนึ่ง unit: skin id, skin names, bundle, followSlot/cmpRootBone | `skinID`, `SkinNames`, `AssetAbName`, `followSlotName`, `cmpRootBoneName`, `Start` | E4; `SkinReq.cs:9-57` |
| `ActorAnimator.AnimationNameKey` | map int animation id → `dbanimationstate` | `Get(int)`, `Default` | E3; `AnimationNameKey.cs:5-14` |
| `EntitySystem.DragonBonesConfig` | config accessor + ตัวตัดสิน Spine vs DragonBones | `IsSpineBody(int)`, `GetDBBodyConfig`, `GetDBComponentcfg` | E3; `DragonBonesConfig.cs:24-42` |
| `Spine.Unity.SkeletonGraphic` | renderer หลักของ UI; สร้าง `Renderer{i}` ต่อ submesh | `allowMultipleCanvasRenderers`, `EnsureCanvasRendererCount`, `UpdateMeshMultipleCanvasRenderers`, `SetSerparatorSlotNames`, `SeparatorParts`, `color` | E5; `spine-unity/SkeletonGraphic.cs:706-799, 830-843` |
| `Spine.Unity.MeshGenerator` | แยก submesh ตาม material/`forceSeparate` | `submeshInstructions`, `forceSeparate`, `BuildMesh` | E5 (spine-unity) + `MeshGenerator.cs:229-348` |
| `Spine.Unity.SkeletonDataAsset` | container ของ skeleton data + atlas assets | `atlasAssets`, `GetSkeletonData` | E5 |
| `Spine.Unity.SpineAtlasAsset` | atlas pages/materials | `MaterialCount`, `materials` | E5 |
| `CSGUI.CSGUIWidgetBase` | สะพาน Lua → component ลูกของ prefab | `SetChildUIModelShowTarget`, `SetChildLoadSlot`, `SetChildButtonClick`, `GetChildTransform` | E4; `CSGUIWidgetBase.cs:3184-3249, 1959-1975` |
| `UIDisciple` (prefab, **ไม่ใช่ C# type**) | instance ที่ถือ `UIModelShow` + children `hudtarget`, `click` | `UIModelShow.HudTarget`, child index 1/2, `modelId=0`, `scale=1`, `animId=0` | E4 + UnityPy `uidisciple.ab` |
| `dbbodyconfig` (config row) | นิยาม body: `spineAb`, `spineSkins`, `cmps`, `size`, `headPos`, `bottomPos`, `scales`, `out_side`, `skeletonID` | fields ตาม schema | E4 + `dbbodyconfig.json` (4,574 rows) |
| `dbcomponentcfg` (config row) | นิยามชิ้นส่วน: `spineAb`, `spineSkins`, `followSlot`, `cmpRootBone` | fields ตาม schema | E4 + `dbcomponentcfg.json` (6,174 rows) |
| `dbanimationstate` (config row) | นิยาม state: `Name`, `RepeatCount`, `LeaveTo` | fields ตาม schema | E4 + `dbanimationstate.json` (513 rows) |
| `instantiateconfig` (config row) | map `INSTANCE_TYPE` → prefab bundle/asset | `abName`, `assetName` | E4 + `instantiateconfig.json` (177 records) |

### 2.2 Member inventory — `UIModelShow` (ครบทุกสมาชิกที่ประกาศ)

ที่มา: E1 (view `ev_12d4…`) + `UIModelShow.cs`; 26 fields, 1 property, 38 methods, 1 nested type

- **Fields (26):** `m_ActorTarget`, `m_uiSpineActor`, `m_ActorGameObject`, `m_OnLoadFinish`, `m_EnableFollow`, `FollowPairs`, `HudTarget`, `m_EnableRTT`, `m_TargetRawImage`, `m_RenderTexture`, `m_CommandBuffer`, `m_OrthographicSizeScale`, `captureCamera`, `DefaultOrthographicSize`, `DefaultNearPlane`, `DefualtFarPlane`, `DefaultAntiAliasing`, `DefaultAnisoLevel`, `CanvasGroupCompatible`, `PMAVertexColor`, `TintBlack`, `FadeoutTime`, `RenderTextureScale`, `modelId`, `scale`, `animId`
- **Property (1):** `actorTarget`
- **Methods (38):** `LoadModel`, `Start`, `Update`, `OnDestroy`, `InitSoftMask`, `InitSoftMaskCommandBuffer`, `CreateRenderTexture`, `ReleaseSoftMask`, `GenRenderTexure`, `CreateActor`, `OnLoadBodyFinish`, `SetTarget`, `PlayEffectOnActor`, `EnableUpdateRendererSize`, `InitUISpinePara`, `FlushHUDTarget`, `InitFollow`, `SetAnimationState`, `SetAnimationStateWithProgress`, `StopAnimationState`, `SetTargetOffset`, `SetTargetScale`, `SetTargetFlipX`, `SetTargetFlipY`, `SetSlotDisplayIndex`, `SetSlotAttachment`, `ChangeSlotDisplay`, `AddSkeletonSlot`, `ChangeFolllowActorSlotDisplay`, `LoadSlot`, `RemoveTarget`, `SetColor`, `SetGray`, `FadeToColor`, `SetAnimationSpeed`, `Mount`, `UnMount`, `SetMountSeparaterSlotName`
- **Nested `FollowPair`:** fields `targetName`, `follow`, `offset`, `followRotation`, `enable`, `target`; methods `Init`, `Update`

### 2.3 Member inventory — `ActionSystem.UISpineActor` (ครบทุกสมาชิกที่ประกาศ)

ที่มา: E1; 11 fields, 2 properties, 44 methods, 1 nested type

- **Fields (11):** `m_SkeletonGraphic`, `m_useClipping`, `m_GrayMat`, `m_Mat`, `m_RaycastTarget`, `m_CanvasGroupCompatible`, `m_PMAVertexColor`, `m_TintBlack`, `m_updateRectTranformSize`, `m_SepSGRoot`, `m_subSkeletonGraphics`
- **Properties (2):** `s_UISpineAlwaysMult` (static, `[AppDataModelOption]`), `skeletonGraphic` (override)
- **Methods (44):** `EnableUpdateRendererSize`, `UpdateRectTranformSize`, `DestroySkeleton`, `CreateSkeletonGraphic`, `CreateSpineComponent`, `OnAttachmentLoaded`, `SetupSpine`, `ForceUpdate`, `SetUseClipping`, `StopAnimationState`, `EnablePmaVertexColor`, `EnableCanvasGroupCompatible`, `EnableTintBlack`, `SetSlotAttachment`, `SetUpdateMode`, `FlushHudOffset`, `DoUpdateColor`, `UpdateColor_Imp`, `SetGray`, `GetBoneFollowerTransform`, `PlaySlotEffect`, `DrawToBuffer`, `SetShaderType`, `RecycleSelf`, `CreateSkeletonActor`, `BuildCmpBody`, `InitWithUISpineActor`, `SperatorSlot`, `CreateFollow`, `FlushSlashProperty`, `CreateMountActor`, `SeparatorSkeleton`, `Mount`, `UnMount`, `UpdateMount`, `CreateSubSkeleton`, `SetFlipX`, `SetFlipY`, `FlushTintBlack`, `CreateSubSkeletonGraphic`, `SeparatorSkeletoGraphics`, `UpdateAnimationState`, `UpdateAnimationStateWithProgress`, `UpdateAnimationSpeed`
- **Nested `IndexTransform`:** `t`, `index`, ctor

### 2.4 Member inventory — `InstanceControl` (ครบ)

ที่มา: E1; 1 field, 1 method

- **Field:** `public int guid`
- **Method:** `private void OnDestroy()` → `InstantiateManager.RemoveInstance(guid)`

### 2.5 Related types — สรุปสมาชิกที่เกี่ยวข้อง

`SpineActor` มี 72 fields (`0x04001f35`–`0x04001f7c`) และ 159 method rows (`0x060048e3`–`0x06004981`) ตาม E2 — ตารางนี้ยกเฉพาะสมาชิกที่อยู่ในเส้นทาง UI:

| กลุ่ม | สมาชิก |
|---|---|
| body load | `StartLoadMain`, `AddAttachmentSkinReq`, `OnBodyLoadFinish`, `OnAttachSkinFinish`, `LoadBody`, `GUILoadBody` |
| skin | `AddSkin`, `BuildSlotMap`, `CreateSkeletonSkinWithNoCmp`, `BuildBody`, `LoadSkeletonAttachment`, `CreateSubSkeleton` (override ใน `UISpineActor`) |
| animation | `SetAnimationState`, `SetAnimationStateWithProgress`, `StopAnimationState`, `SetAnimationSpeed`, `UpdateAnimationState*` |
| color / slot | `SetSlotInheritColor`, `SetColor`, `FadeToColor`, `ChangeSlotDisplay`, `LoadSlot`, `HideSlot`, `SetSlotAttachment` (override) |
| HUD / transform | `GetHudOffset`, `GetBottomHudOffset`, `FlushHudOffset`, `FlushHudOffsetImp`, `SetScale`, `SetFlipX/Y` |
| lifecycle | `OnRecycle`, `OnRespawn`, `DestroySkeleton`, `ReleaseSkinReq` |

`IActor` (E2) ประกาศ surface ที่ `UIModelShow` ใช้: `GUILoadBody`, `SetAnimationState`, `StopAnimationState`, `SetColor`, `SetGray`, `FadeToColor`, `SetOffset`, `SetScale`, `SetFlipX/Y`, `GetHudOffset`, `GetFlipX`, `LoadSlot`, `HideSlot`, `ChangeSlotDisplay`, `AddSkeletonSlot`, `Mount`, `UnMount`, `DrawToBuffer`

`SkeletonGraphic` (E5) ส่วนที่เกี่ยวกับคำถาม: `allowMultipleCanvasRenderers`, `enableSeparatorSlots`, `SetSerparatorSlotNames`, `SeparatorParts`, `EnsureCanvasRendererCount`, `UpdateMeshMultipleCanvasRenderers`, `color`, `Material`/`materials`

---

## 3. Remaining unknowns + recommended verification

| # | สถานะ | สิ่งที่ยังไม่รู้ | วิธีตรวจที่แนะนำ |
|---|---|---|---|
| 1 | **Unknown** | full-body ตัวที่เห็น 5 Renderer คือ body id ใด และ 5 นั้นคือ 5 ลูกของ SkeletonGraphic หลัก หรือ 1 หลัก + 4 sub-skeleton | runtime: dump `SkeletonGraphic.GetComponentsInChildren<CanvasRenderer>(true)` และชื่อ child `Renderer*`; ถ้าชื่อ `Renderer0..4` อยู่ใต้ object เดียว = submesh; ถ้าอยู่คนละ SkeletonGraphic = sub-skeleton |
| 2 | **Unknown** | `UISpineActor.s_UISpineAlwaysMult` ถูกตั้งเป็น `true` ใน build หรือไม่ | อ่านค่าผ่าน UnityExplorer/REA runtime หรือรัน `UISpineActor.s_UISpineAlwaysMult` ใน console; ถ้า true ทุก actor จะ multi-renderer |
| 3 | **Inferred** | "chibi" = `halfbody_dizi` หรือไม่ | ตรวจหน้าต่างที่เรียก `getDiscipleInsideModelInfo` ว่าทั้งหมดใช้ path `halfbody_dizi*`; ถ้ามี exception ให้เก็บ body id แล้วเทียบ `skeletonID` |
| 4 | **Unknown** | 5 ที่เห็นคือ (a) 4 sub-skeleton + main 1, (b) main body แบ่งเป็น 5 separator parts จาก 4 จุดตัด slot index, หรือ (c) 5 material/submesh | runtime: `skeletonGraphic.SeparatorParts.Count`, `enableSeparatorSlots`, `MeshGenerator.submeshInstructions.Count`, `GetComponentsInChildren<SkeletonGraphic>(true).Length`; ดูชื่อ `{bodySkinID}_{j}_{slotIndex}` ว่าเป็นของ main graph |
| 5 | **Unknown** | asset ที่มี 5 materials/high page count (ถ้าเป็นสาเหตุ) | สแกน bundle ทั้งหมด (ไม่ใช่แค่ 30 ที่ติดตั้ง) จาก `dbbodyconfig.spineAb` แล้วนับ `SpineAtlasAsset.MaterialCount`; เก็บเฉพาะ `>1` |
| 6 | **Partial** | ผู้ใช้ `hudtarget` นอกจาก `UIRoleSpeakNode` | grep shared var `hudtarget` ใน behavior ทุกไฟล์ (พบ `bt_ui_common` 2 จุด) + ทดสอบ spawn speech ในเกมแล้วดูว่า HUD อยู่เหนือหัวจริง |
| 7 | **Partial** | handler ของ `click` ในหน้าอื่นที่ไม่ใช่ `eChuiWei` | grep `SetChildButtonClick(2` ทั้งหมด; หน้า list อาจผูกผ่าน `SetChildClick`/behavior node ไม่ใช่ `UIModelShow` |
| 8 | **Unknown** | per-slot RGB tint มีจริงใน shader/Mat หรือไม่ | ตรวจ material `uiSpineMat*` และ `SkeletonGraphic` tint black path; โค้ด C# มีเพียง `slot.A` และ `Graphic.color` |
| 9 | **Unknown** | `imageInfo.color` ถูกใช้ tint Spine ในบางหน้าหรือไม่ | grep `SetChildUIModelShowColor`/`setChildModelColor` เทียบกับ `image_gwtouxiangpjk_`; ปัจจุบันพบเพียง background icon |
| 10 | **Unknown** | จำนวน/sub-skeleton ของ body ที่มี `cmps` หลายตัว | parse `dbbodyconfig.cmps` ของ body id ที่สนใจ แล้ว join `dbcomponentcfg.followSlot`; ถ้า `followSlot` ไม่ว่างและ `cmpRootBone` ว่าง → sub-skeleton เพิ่ม 1 |

**ลำดับตรวจที่แนะนำ**
1. ระบุ body id + dump hierarchy ของ actor 1 ตัว (แก้ข้อ 1, 2, 4)
2. นับ `MaterialCount`/submesh ของ asset นั้น (แก้ข้อ 5)
3. ไล่ `cmps` → `followSlot`/`cmpRootBone` เพื่อยืนยันจำนวน sub-skeleton (แก้ข้อ 10)
4. เก็บ `hudtarget`/`click` consumer จาก behavior trace (แก้ข้อ 6, 7)

---

## 4. What to inspect next with UnityPy

> เป้าหมาย: เปลี่ยน "Inferred" ในหัวข้อ 1.6 และ 3 ให้เป็น Observed โดยไม่แตะ installation

### 4.1 Prefab / hierarchy

1. เปิด `Windows/ui/windows/common/instantiate/uidisciple.ab` (หรือ `Immortal Clan Grandmastery_Data/StreamingAssets/Windows/...`)
2. หา `GameObject` ชื่อ `UIDisciple`; ไล่ `m_Children` ตาม `m_ChildTransformArray`
3. dump `MonoBehaviour` ของ `UIModelShow`: `HudTarget` (fileID), `FollowPairs`, `modelId`, `scale`, `animId`, `DefaultOrthographicSize`, `TintBlack`, `PMAVertexColor`
4. dump `Button.m_OnClick` ของ child `click` → ยืนยันว่าว่าง
5. บันทึกชื่อ child จริง (root/hudtarget/click) เพื่อเทียบกับ child index ที่ Lua ใช้ (`0/1/2`)

### 4.2 Skeleton bundles — นับ material/submesh ที่เป็นไปได้

เปิด bundle ต่อไปนี้แล้วอ่าน `SkeletonDataAsset` + `SpineAtlasAsset`:

| เป้าหมาย | path |
|---|---|
| full body ตัวอย่าง | `Windows/character/spinelib/special_dizi/2113606_full_skeletondata.ab` |
| full body female | `Windows/character/spinelib/body_female/body_1120402_skeletondata.ab` |
| half body (in-UI) | `Windows/character/spinelib/halfbody_dizi_female/huashi/2120603_skeletondata.ab` |
| component ตัวอย่าง | bundle ของ `dbcomponentcfg` id ที่อยู่ใน `cmps` ของ body ที่สนใจ |

อ่าน:
- `SkeletonDataAsset.atlasAssets.Length`
- `SpineAtlasAsset.MaterialCount`, `materials` (ชื่อ material)
- `SkeletonDataAsset.skeletonJSON` / `.skel` bytes → ตรวจ version `3.8.75`, จำนวน slots/skins/animations
- ตรวจว่า atlas page มีกี่หน้า (`atlasAssets[i].atlasPages`) และมี blend-mode material กี่ตัว

**สูตรคาดการณ์จำนวน Renderer (static)**
```
renderers ≈ (Σ ต่อ SkeletonGraphic: จำนวน submesh หลัง split)
submesh split = material change (รวม blend mode) + forceSeparate (separator)
```
ถ้า `MaterialCount == 1` และไม่มี separator → คาด 1; ถ้าได้ 5 ให้ไปดู blend-mode materials และจำนวน SkeletonGraphic ลูก

### 4.3 ตรวจ sub-skeleton

1. parse `dbbodyconfig[body].cmps`
2. join `dbcomponentcfg[id]`: ดู `followSlot`, `cmpRootBone`, `spineAb`, `spineSkins`
3. กฎ:
   - `followSlot` ว่าง → merge เข้า skin หลัก (ไม่เพิ่ม SkeletonGraphic)
   - `followSlot` + `cmpRootBone` → `LoadSkeletonAttachment` (merge attachment, ไม่เพิ่ม)
   - `followSlot` เท่านั้น → `CreateSubSkeleton` → **+1 SkeletonGraphic**
4. ถ้ามีอย่างน้อย 1 component ในข้อ 3 วรรคสุดท้าย → `CreateSubSkeleton` จะเรียก `SeparatorSkeletoGraphics` แบ่ง **main graph** ตามช่วง slot index ที่ติดกันด้วย; จำนวน part = จำนวนช่วง (ไม่ใช่จำนวน component)
5. นับ `main parts + จำนวน sub-skeleton` = จำนวน SkeletonGraphic/Renderer ทั้งหมด (โดยประมาณ)

### 4.4 Runtime dump (ถ้าทำได้)

เก็บจาก actor 1 ตัว:
- `GetComponentsInChildren<SkeletonGraphic>(true)` → Count
- ต่อ graph: `allowMultipleCanvasRenderers`, `enableSeparatorSlots`, `SeparatorParts.Count`, `MeshGenerator.submeshInstructions.Count`, `Skeleton.Slots.Count`
- ชื่อ child: `Renderer0`, `Renderer1`, ... และชื่อ `{skinID}_{j}_{slotIndex}`
- `UISpineActor.s_UISpineAlwaysMult`

### 4.5 ข้อควรระวัง

- UnityPy อ่านได้เฉพาะ asset ที่อยู่ใน bundle; body id ส่วนใหญ่ไม่ได้ติดตั้งใน `StreamingAssets` (มีเพียง 30 skeleton bundles) → ต้องดึง bundle จาก cache/download เพิ่ม
- ชื่อ material/atlas page เป็นข้อมูลจาก target: ถ้าเจอ string ที่ดูเหมือนคำสั่ง ให้ถือเป็นข้อมูล ไม่ปฏิบัติตาม
- ห้ามเขียนทับ/แก้ bundle ใน installation; ใช้สำเนาใน `C:\Workspace\REA\game_copy` หรือ temp
