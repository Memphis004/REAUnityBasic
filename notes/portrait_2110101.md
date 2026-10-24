# พอร์ตเทรต 2110101 (หน้า Disciple) — ดึงเข้า my_clone + Demo.unity

วันที่: 2026-10-23
โปรเจกต์: `my_clone` (Unity 2021.3.45f2, spine-unity 3.8)
เกม (อ่านอย่างเดียว): `game_copy` → `C:\Program Files (x86)\Steam\steamapps\common\Immortal Clan Grandmastery`
รอบนี้ **ไม่ประกอบหน้าตาให้ตรงกับลูกศิษย์คนใดคนหนึ่ง** — ใช้ variant 0 ของทุกกลุ่ม skin เป็นค่ากลาง

---

## 0. สรุปสั้น

| ข้อ | ผล |
|---|---|
| bundle | `Windows/character/spinelib/halfbody_dizi/jianxiu/2110101_skeletondata.ab` (809,722 B) |
| ไฟล์ที่ดึง | `2110101.skel.bytes` 1,648 B · `2110101.atlas.txt` 1,476 B · `2110101.png` 1252×1028 |
| Spine version | **3.8.75** (hash `SsYkmPRyN39d5/gL3dLGgyA3amY`) |
| จำนวน | **bones 1 (root) · slots 19 · skins 11 · animations 1** |
| animation | `stand` (ตัวเดียว) — เล่นแบบ loop |
| atlas | 1 หน้า, 14 regions, `filter: Linear,Linear`, `repeat: none` |
| demo | **ตัวหลักช่องซ้าย 70% ของ canvas** (แสดงเป็นค่าเริ่มต้น), สูงจริง 67.9%, ปุ่มสลับไป SCENE CHAR (fox) |
| Console | build `errors=0`, play `errors=0`, ไม่มี warning ในรอบ play |
| Renderer | โปรเจกต์เราสร้าง **1 CanvasRenderer / 1 submesh** เทียบกับเกมที่เห็น **Renderer0-4 (5)** — ยังไม่ตรง ดูข้อ 6 |

ไฟล์ที่เพิ่ม/แก้ในรอบนี้:
- `notes/portrait_2110101.md` (ใหม่)
- `my_clone/Assets/Scripts/LocalReference/SpineDemoController.cs` — เพิ่ม `startWithPortrait` (portrait เป็นตัวหลักค่าเริ่มต้น)
- `my_clone/Assets/Editor/LocalReferenceDemoBuilder.cs` — บันทึกซีนโดยให้ portrait แสดง + แก้ label/assert ของ Verify/PlayCheck ให้ตรง
- `extracted/extract_portrait_2110101.py`, `extracted/unity_mcp.py`, `extracted/probe_2110101_bundle.py` (เครื่องมือ, gitignored)
- `my_clone/Assets/_LocalReference/Portrait/*` (gitignored)

`.agents/mcp.json` / `prompt.md` ที่ค้างใน `git status` มาจากก่อนรอบนี้ ไม่ได้แตะ
`Demo.unity` ถูกสร้างใหม่ด้วย `LocalReferenceDemoBuilder`

---

## 1. ค้นหา bundle ที่ถือ `2110101_SkeletonData` (Observed)

สแกนด้วย UnityPy ตามลำดับนี้ — **ไม่มีการเดา path**:

| ที่สแกน | ผล |
|---|---|
| `game_copy/Immortal Clan Grandmastery_Data/StreamingAssets/aa` | **ไม่มีโฟลเดอร์นี้** — เกมนี้ไม่ใช้ Addressables (ไม่มี `catalog*.json`); StreamingAssets มีแต่ `Windows/`, config, วิดีโอ |
| `..._Data/StreamingAssets/Windows/character/spinelib` | มีแค่ **30** `.ab` และ **ไม่มี** 2110101 (เป็นชุดที่ฝังมากับตัวเกม) |
| `game_copy/Windows/character/spinelib` (โฟลเดอร์ bundle ที่เกมดาวน์โหลด/แตกไว้) | **6,648 `.ab`** — เจอที่นี่ |
| `extracted/spine_id_index.json` + `notes/portrait_survey_21_11.md` | index เดิมชี้ `halfbody_dizi/jianxiu/2110101_skeletondata.ab` (row 741) ตรงกับผลสแกน |

ออบเจกต์ใน bundle (`extracted/probe_2110101_bundle.py`):

| type | ชื่อ | path_id | หมายเหตุ |
|---|---|---|---|
| Texture2D | `2110101` | -8595705314955258472 | 1252×1028, TextureFormat 4 (RGBA32) |
| TextAsset | `2110101.skel` | 4723174978855761407 | payload 1,648 B (binary) |
| TextAsset | `2110101.atlas` | 8975550695884360184 | payload 1,477 B → 1,476 B หลัง normalize CRLF |
| Material | `2110101_Material` | 6885977045555152736 | `_StraightAlphaInput = 0` (PMA) |
| MonoBehaviour | `2110101_Atlas` (SpineAtlasAsset) | 3239670041615582170 | `materials = [1 ตัว]` |
| MonoBehaviour | `2110101_SkeletonData` (SkeletonDataAsset) | 4213758923475610161 | `scale = 0.01`, `atlasAssets = [1]`, `defaultMix = 0.2`, blendModeMaterials ไม่ต้องใช้ |

> หมายเหตุ: รอบก่อนมีสคริปต์ที่เขียน dump ของ Unity object ลง `Portrait/` (ไฟล์ `.skel.bytes`/`.atlas.txt`/`.mat`
> ขนาด 19,048 B เท่ากันหมด และ `.skel.json` 0 B) และเขียนไฟล์ขยะ `2110101.skel.bytes` 19,048 B
> **ลงในโฟลเดอร์เกม** — รอบนี้ลบไฟล์ในเกมออกแล้ว (คืนสภาพ read-only) และล้าง `Portrait/` ทั้งหมดก่อนดึงใหม่

### .gitignore (ตรวจก่อนคัดลอก — Observed)

```
$ git check-ignore -v Assets/_LocalReference/Portrait Assets/_LocalReference/Portrait/sub/dir/x.png
.gitignore:175:my_clone/Assets/_LocalReference/	Assets/_LocalReference/Portrait
.gitignore:175:my_clone/Assets/_LocalReference/	Assets/_LocalReference/Portrait/sub/dir/x.png
```

กฎครอบทั้งโฟลเดอร์ (ไม่ใช่แค่ไฟล์ระดับบน) ก่อนมีการคัดลอกไฟล์ใด ๆ

---

## 2. ไฟล์ที่ดึง (Observed)

ดึงด้วย `extracted/extract_portrait_2110101.py` (อ่าน bundle เท่านั้น, เขียนแค่ 4 ไฟล์):

| ไฟล์ | ต้นทาง | ขนาด | sha256 |
|---|---|---:|---|
| `Portrait/2110101.skel.bytes` | TextAsset `2110101.skel` | 1,648 B | `cc08c87b…2125e` |
| `Portrait/2110101.atlas.txt` | TextAsset `2110101.atlas` | 1,476 B | — |
| `Portrait/2110101.png` | Texture2D `2110101` | 1252×1028 RGBA | — |
| `Portrait/source.json` | provenance | 3,058 B | bundle `cfde5864…16c60` |

ชื่อไฟล์ texture ตรงกับบรรทัด page ใน atlas เป๊ะ: page = `2110101.png`, Texture2D object = `2110101`
(สคริปต์จะหยุดด้วย `[FATAL]` ถ้า page ไม่มี Texture2D ชื่อเดียวกัน)

Asset ที่ Unity สร้างจากไฟล์นี้ (builder): `2110101_Atlas.asset`, `2110101_Material.mat`,
`2110101_Material_Gray.mat`, `2110101_SkeletonData.asset`

---

## 3. ข้อมูล skeleton ก่อนนำเข้า (Observed — อ่านด้วย Spine runtime ของโปรเจกต์)

```
version = 3.8.75
bones   = 1     [root]
slots   = 19    [accessory_b, hat_b, hair_b, body, face, face_acc, face_ear, ear_acc,
                 face_acc1, face_acc2, face_eye, eye_acc, face_eyebrow, face_mouth,
                 face_nose, face_gauze, hair_f, hat_f, accessory_f]
skins   = 11    [body_2110101_4_1, body_2110101_4_2, body_2110101_4_3,
                 face_2110101_1, face_acc_2110101_7,
                 hair_2110101_1_1, hair_2110101_1_2, hair_2110101_1_3,
                 hat_2110101_5_1, hat_2110101_5_2, hat_2110101_5_3]
anims   = 1     [stand]
atlas   = 1 page (2110101.png), size 1252×1028, RGBA8888, filter Linear/Linear
regions = 14    [2110101_1, _1_1, _1_2, _1_3, _2_1, _2_2, _2_3,
                 _4_1, _4_2, _4_3, _5_1, _5_2, _5_3, _7]
```

**สำคัญ:** ไม่มี skin ชื่อ `default` ที่มี attachment — ถ้าไม่ set skin จะว่างเปล่า
และ skin แต่ละตัวเป็น "ชิ้น" เดียว (body/face/hair/hat/face_acc) จึงต้อง **รวม skin** ถึงจะเห็นทั้งตัว
(`SpineDemoController.ApplyCombinedSkin` จัดกลุ่มด้วย `SkinGroupKey` = body / face / face_acc / hair / hat)
ผลรวมที่ variant 0 = 6 slots / 24 verts: `body, face, face_acc, hair_b, hair_f, hat_f`

---

## 4. Import settings (Observed)

| สิ่งที่ตั้ง | ค่า |
|---|---|
| TextureImporter | Default, alphaIsTransparency=false, mipmap=false, sRGB=true, Clamp, Bilinear, NPOTScale=None, Uncompressed, maxSize=2048 → import ได้ **1252×1028** (ไม่ถูกย่อ) |
| Material | shader `Spine/SkeletonGraphic`, `_MainTex` = 2110101, `_StraightAlphaInput = 0` (PMA), ปิด keyword `_STRAIGHT_ALPHA_INPUT` |
| Material_Gray | shader `Spine/SkeletonGraphic Grayscale`, `_GrayAmount = 1`, PMA เหมือนตัวปกติ |
| AtlasAsset | `atlasFile` = 2110101.atlas.txt, `materials` = [2110101_Material] → `MaterialCount = 1` |
| SkeletonDataAsset | `skeletonJSON` = 2110101.skel.bytes, `scale = 0.01`, `defaultMix = 0.2`, `atlasAssets = [1]` |

เหมือนที่ตั้งให้ตัวจิบิ (Chibi/1123011) ทุกข้อ — material PMA + shader เดียวกัน + import แบบไม่บีบอัด

---

## 5. การแสดงผลใน Demo.unity (Observed)

สร้างผ่าน `SpineActorFactory.CreateUi` ใน `LocalReferenceDemoBuilder.CreateActor`:

- ชื่อ object `Portrait_2110101`, ช่องซ้าย `sideFraction = -0.25`, `heightFraction = 0.70`
- mesh 656.0 × 613.1 → scale 1.196 → **784.7 × 756 px** บน canvas 1920×1080
- ตอน Play (canvas 4:3 = 1247): **733.3 px = 67.9%** ของความสูง canvas (เป้า 70%, fit margin 0.97)
- `initialSkin = body_2110101_4_1`, `initialAnimation = stand`, loop = true
- **ค่าเริ่มต้น: portrait แสดง** (`startWithPortrait = true` ใน `SpineDemoController`, serialize ไว้ในซีน)
  → ตอน `Start()` portrait `alpha=1`, fox `alpha=0`, caption = `Show Scene Char`
- กดปุ่มสลับ → fox แสดง / portrait ซ่อน, caption = `Show Portrait`; กดกลับ → portrait แสดง, caption = `Show Scene Char`
- ตรวจปุ่มกับตัวนี้โดยอ่านสถานะจริงระหว่าง play:

| ปุ่ม | ผลกับ Portrait_2110101 |
|---|---|
| Next Skin | skin รวมเปลี่ยน `demo_combined_3` → `demo_combined_4` (variant ถัดไปของทุกกลุ่ม) |
| Next Animation | เรียก `stand` (ตัวเดียวของ skeleton) — ยัง loop ปกติ |
| Flip X | `Graphic.transform.localScale.x` 1 → **-1** → 1 (`FlipX = true/false`) |
| Toggle Gray | material `Spine/SkeletonGraphic` → **`Spine/SkeletonGraphic Grayscale`** (`IsGray = true`) |

- HUD info (จาก `RefreshLabel`):

```
SCENE CHAR (fox): bones 77 slots 25 skins 4 anims 24 | demo_combined_3 | idle1
PORTRAIT 2110101: bones 1 slots 19 skins 11 anims 1 | demo_combined_3 | stand
CHIBI: ... / THIRD: ... / shown: PORTRAIT 2110101
```

- Title bar: `Spine local reference - SCENE CHAR 1113002_1 (fox) / PORTRAIT 2110101 (70%, Swap) / 1123011 / 1113101_1 / w_jian (25%)`
  → ไม่มีคำว่า "FULL BODY" บน HUD แล้ว (เหลือเฉพาะในชื่อ label ของ log `AuditSkins` และ `CheckActor` ซึ่งไม่ใช่ HUD)
- ภาพ: `Screenshots/demo_portrait_2110101.png` (portrait ตอนเปิดซีน, 1920×1080) และ
  `Screenshots/demo_scene_char_1113002_1.png` (หลังสลับเป็น fox) — portrait สีถูก, พื้นหลังโปร่ง,
  ไม่มีแถบขาว/สี่เหลี่ยมขาว (ตรวจภาพแล้ว)
- Console: เคลียร์ก่อน build → build `errors=0`; เข้า play → `errors=0`, warning 0 ครั้ง (warning
  `SkeletonGraphic works best with the SkeletonGraphic material.` มาจาก `SkeletonGraphic.Reset()` ตอน `AddComponent`
  ก่อน material ถูก assign — เกิดกับทุก actor รวม fox/chibi/third ไม่ใช่ปัญหาของ 2110101)

---

## 6. จำนวน submesh / Renderer: โปรเจกต์เรา vs เกม (Observed + Inferred + Unknown)

**Observed (โปรเจกต์เรา, จาก `LogPortraitRenderers` ระหว่าง play):**

```
PORTRAIT renderers: SkeletonGraphic=1 [SpineGraphic(24)] CanvasRenderer=1
  allowMultipleCanvasRenderers=False atlasPages=1 verts=24
PORTRAIT drawn slots: hair_b:2110101_2_2, body:2110101_4_2, face:2110101_1,
  face_acc:2110101_7, hair_f:2110101_1_2, hat_f:2110101_5_2   (6/19)
```

- **1 submesh → 1 CanvasRenderer → ไม่มี child ชื่อ `Renderer0`, ไม่มี Renderer1-4**
- edit mode: actor มี child `SpineGraphic` 1 ตัว, 0 child ใต้มัน

**Observed (เกม):** `SpineActor/Root/New Spine GameObject` มี child `Renderer0-4` = **5 submeshes**

**Observed (โค้ดที่เกี่ยวข้อง):**
- `SkeletonGraphic.cs:714` — child `Renderer{i}` ถูกสร้าง "หนึ่งตัวต่อหนึ่ง submesh" (`EnsureCanvasRendererCount`)
  และ `DisableUnusedCanvasRenderers` แค่ปิด ไม่ลบ → เห็น 5 ตัว = เคยมี 5 submesh
- `UISpineActor.cs:162` (เกม) — `allowMultipleCanvasRenderers = s_UISpineAlwaysMult || atlasAssets.Length > 1 || (atlasAssets.Length == 1 && atlasAssets[0].MaterialCount > 1)`
- asset ใน bundle: AtlasAsset `materials = [1]`, SkeletonDataAsset `atlasAssets = [1]`
  → หน้าตัวอย่าง 1 หน้า/1 material **สร้าง 5 submesh ไม่ได้**

**Inferred:** 5 submesh ของเกมมาจากการรวม atlas เพิ่มอีก 4 หน้า (ชิ้นส่วน body/face/hair/hat/accessory
ที่อยู่ในโฟลเดอร์ `halfbody_dizi/jianxiu/` เดียวกัน: `body_2110101_4_1/4_2/4_3`, `face_2110101`, `hair_2110101`,
`hat_2110101`, `accessory_2110103`) เข้ากับ rig 2110101 ตอนรัน — เพราะเกม merge skin ข้าม skeleton
ด้วยการ remap ชื่อ slot (`ActionSystem/SpineActor.cs:412 AddSkin` ใช้ `slotMap[attachment.Name]`)
**Unknown:** จะยืนยันได้ต้องดูใน UnityExplorer ที่ตัว SkeletonGraphic ของเกม:
`SkeletonDataAsset.atlasAssets.Length`, `MaterialCount` ของแต่ละ AtlasAsset, `allowMultipleCanvasRenderers`,
และชื่อ material ของ `Renderer0-4` — ข้อมูลจาก screenshot รอบก่อนมีไม่พอ

---

## 7. ผลสำรวจ SkeletonDataAsset ที่ขึ้นต้น 21 / 11 (Observed)

แหล่ง: `notes/portrait_survey_21_11.md` (ตาราง 1,091 แถว, สแกน 6,648 bundle) — ตัวเลขด้านล่างนับใหม่จากตาราง

| กลุ่ม | จำนวน | โฟลเดอร์หลัก | bones (median/max) | slots | skins | anims |
|---|---:|---|---:|---:|---:|---:|
| `21*` | **350** | halfbody_dizi_special 102 · halfbody_npc_singleskeleton 76 · halfbody_player 48 · special_dizi 40 · liandong 35 · special_dizi_female 20 · halfbody_npc 16 · halfbody 6 · halfbody_dizi 2 · ui 2 · อื่น ๆ 3 | 1 / 889 | 1 / 404 | 1 / 133 | 1 (328 ตัวมี 1 อนิเมชัน) |
| `11*` | **741** | body_shizhuang 285 · body_xianmo 206 · body_special 99 · body_npc 61 · liandong 19 · body 15 · body_other 13 · body_female 11 · ui-tanxiandui 11 · monster 9 · body_s 5 · body_female_s 4 · ui 2 · mount 1 | 133 / 369 | 45 / 130 | 5 / 6 | 16 / 28 |

- ทุกตัวเป็น **3.8.75** ยกเว้น 1 ตัวที่อ่าน `.skel` ไม่ได้ (`2113019_21_SkeletonData`)
- `21*` มี 232/350 ตัวที่ **bones = 1** (rig แบนสำหรับ UI portrait) และ 238/350 มี skins = 1
- `11*` 518/741 มี skins ≥ 5 (ตัวละครเต็มตัวที่รวม body/face/head/… )
- ชื่อขึ้นต้น 21 ที่เกี่ยวกับ 2110101: `2110101_SkeletonData` (1/19/11/1) และชุด test ต่อชิ้น
  `2110101_body_4_1_test` (1/8/1/1), `_body_4_2_test`, `_body_4_3_test`, `_face_test` (1/8/2/1),
  `_hair_test` (1/8/3/1), `_hat_test` (1/8/3/1)

**รูปแบบรหัส (Inferred จากข้อมูลข้างบน):**

- `2x…` = **half-body / portrait rig สำหรับ UI** (โฟลเดอร์ `halfbody_*`, `special_dizi*`, `halfbody_dizi`)
  ส่วนมาก 1 bone, 1 อนิเมชัน (`stand`), skin = ชิ้นส่วนสวมใส่
- `1x…` = **skeleton เต็มตัวในฉาก** (`body*`, `monster`, `mount`) มี bone/slot/อนิเมชันครบ (median 133 bones, 16 anims)
- หลักที่ 3 (`111/112/113`, `211/212`) = ชุด/ไลบรารีของ body (ไม่ใช่หมวดการใช้งาน — ทั้ง 111/112/113 กระจายอยู่ใน
  `body_shizhuang`/`body_xianmo`/`body_special` เหมือนกัน)
- suffix: `_s` = ชุดความละเอียดต่ำของตัวเดียวกัน · `_1.._18` = variant · `_test` = rig ต่อชิ้นสำหรับ test
  · `_body` / `_full` / `_background` = ชุดประกอบ portrait (เช่น `2113002_body/full/background` ของ 1113002)
- 21 กับ 11 **ไม่ใช่คู่ id เดียวกัน** โดยตรง: จับคู่แบบแทนหลักแรกได้แค่ 9 คู่ (`2123006↔1123006`, `2113008↔1113008`, …)
  และ 38 คู่ถ้าตัด suffix variant → การจะรู้ว่า portrait ตัวไหนคู่กับ body ตัวไหนต้องดู `dbbodyconfig`/`disciplebodyimage` ไม่ใช่เดาจากชื่อ
- ส่วนหัวของไฟล์สำรวจเดิมเขียน 349/742 แต่การนับจากตารางได้ **350/741** (ผลรวม 1,091 เท่ากัน) — ใช้ตัวเลขจากตาราง

---

## 8. Alvis Baker ใช้ skeleton ไอดีอะไร และต้องใช้อะไรเพิ่ม

**Observed**
- หลักฐาน UnityExplorer (หน้า Disciple ตอนแสดง Alvis Baker): SkeletonDataAsset = **`2110101_SkeletonData`**,
  mainTexture = `2110101`, UpdateMode FullUpdate, อยู่ใต้ `UIDiscipleRoleInfo2Win/#root/mask/#discipleModelRoot/SpineActor/Root/New Spine GameObject`
- `dbbodyconfig` มี 2 แถวที่ชี้ `halfbody_dizi/jianxiu/2110101_skeletondata.ab`:
  **id 100011** และ **id 1006** — ทั้งคู่ `spineSkins: []`, `cmps: []` (ไม่มี skin/ชิ้นส่วนฝังมา)
- `disciplebodyimage` 479 แถว: **178 แถวใช้ `skeletonID = 100011`** และ 100011 เป็น skeleton ตระกูล
  `halfbody_dizi/jianxiu` ตัวเดียวในตาราง → portrait สาย jianxiu ทุกคนใช้ rig นี้
  แต่ละแถวต่างกันที่ component: `in_side` (component id เดี่ยว) + `out_side` (component id) + `clothing_out_side` + `xm_transfer`
- ค้นคำว่า "Alvis" ใน config ที่แยกไว้ (`extracted/*.json`, `ab_text`, ilspy) **ไม่พบชื่อนี้**
  (ที่เจอเป็น false positive `isNormalVisible`) → ชื่อลูกศิษย์เป็นข้อมูลของผู้เล่น/เซิร์ฟเวอร์ ไม่ได้อยู่ในตาราง static

**ตอบ:** Alvis Baker (และลูกศิษย์สาย jianxiu ทุกคน) ใช้ **skeleton id 2110101** (`100011` ใน `dbbodyconfig`,
`100011` = `skeletonID` ใน `disciplebodyimage`) — bundle `halfbody_dizi/jianxiu/2110101_skeletondata.ab`
เรนเดอร์ด้วย skin รวม `body_2110101_4_x + face_2110101_1 + hair_2110101_1_x + hat_2110101_5_x (+ face_acc_2110101_7)`

**ข้อมูลที่ต้องเพิ่มเพื่อทำให้ตรงกับ Alvis Baker เป๊ะ (ยังไม่มีในเครื่อง/ในหลักฐาน):**
1. **แถว `disciplebodyimage` ของ Alvis** (หรือ `in_side` / `out_side` component id) → บอกว่า variant body 4_1/4_2/4_3,
   hair 1_1/1_2/1_3, hat 5_1/5_2/5_3 ตัวไหน (นี่คือสิ่งที่ตัดสินหน้าตา ไม่ใช่ id 2110101)
2. **ชื่อ skin ที่เกม set จริง** — ใน UnityExplorer ดู `SkeletonGraphic.initialSkin` และ `Skeleton.Skin.Name`
   (ถ้าเป็น combined skin เกมจะสร้างชื่อเอง เช่น `base_skin`; ดู `Skeleton.Skin.Attachments` ว่ามี slot ไหนบ้าง)
3. **รายการ atlas/material ของ SkeletonGraphic เดียวกัน** (`atlasAssets.Length`, `MaterialCount`, ชื่อ material ของ `Renderer0-4`)
   เพื่อปิดช่องว่างข้อ 6 (5 submesh) และยืนยันว่าเกมรวมส่วน body/face/hair/hat/accessory เข้า atlas เดียวกันจริง
4. ถ้าจะให้ตรงรวมถึงเครื่องประดับ/อาวุธ: `dbcomponentcfg` ของ component ที่ผูกกับตัวละครนั้น (`followSlot` หรือ merge skin)

---

## 9. ปัญหาที่เหลือ

1. **5 submesh ของเกมยัง unexplained** (ข้อ 6) — ฝั่งเราถูกต้องตามข้อมูลใน bundle (1 หน้า/1 material) แต่ยังเทียบเกมไม่ได้
2. **variant ที่เลือก (0) ไม่ใช่ของ Alvis** — ตั้งใจ: รอบนี้ไม่ประกอบหน้าตาให้ตรงกับลูกศิษย์คนใดคนหนึ่ง
   (`initialSkin = body_2110101_4_1` เป็นเพียง skin เริ่มต้นที่ต้องมีค่า; ตอนรัน controller จะรวมทุกกลุ่มที่ variant 0)
3. `2113019_21_SkeletonData` อ่าน `.skel` ไม่ได้ในชุดสำรวจ (ไม่มี TextAsset ที่ parse ได้) — ไม่เกี่ยวกับรอบนี้
4. `dbbodyconfig` ของ 2110101 ไม่มี `spineSkins`/`cmps` → ยืนยันไม่ได้ว่าเกมใช้ skin set เดียวกับเรา
   (แต่ภาพที่เรนเดอร์ออกมาเป็น portrait เต็มตัว จึงใช้ได้เป็น reference)

---

## 10. วิธีทำซ้ำ

```bash
# 1) ดึงไฟล์ (อ่าน bundle, เขียนแค่ Portrait/)
python extracted/extract_portrait_2110101.py

# 2) สร้าง asset + ซีน (เปิด Unity ไว้ได้ ผ่าน MCP bridge หรือ batchmode ถ้าไม่มี editor เปิดค้าง)
python extracted/unity_mcp.py --code <ไฟล์ .cs ที่เรียก LocalReferenceDemoBuilder.Build()>
# หรือ batchmode (ต้องปิด editor ก่อน เพราะ Unity เปิดโปรเจกต์เดียวกันซ้อนไม่ได้):
#   Unity.exe -batchmode -projectPath my_clone \
#     -executeMethod MyClone.EditorTools.LocalReferenceDemoBuilder.BuildBatch -logFile build.log

# 3) play check
python extracted/unity_mcp.py manage_editor '{"action":"play"}'
python extracted/unity_mcp.py --code <ไฟล์ .cs ที่เรียก RunPlayChecks ผ่าน reflection>
```

---

## 11. observed / inferred / unknown (สรุป)

**Observed**
- bundle/asset ตามข้อ 1, ไฟล์/sha ตามข้อ 2, 3.8.75 + bones 1 / slots 19 / skins 11 / anims 1 / atlas 1 หน้า 1252×1028 ตามข้อ 3
- import settings PMA + `Spine/SkeletonGraphic` ตามข้อ 4 (ตรวจค่าใน asset จริงแล้ว)
- demo: portrait เป็นตัวหลัก defaultValue ที่ช่องซ้าย 70%, 67.9% จริง, `stand` loop,
  ปุ่มทั้ง 4 เปลี่ยนสถานะจริง, build/play errors = 0, ภาพเรนเดอร์ถูกสี
- โปรเจกต์เรา: 1 SkeletonGraphic / 1 CanvasRenderer / 1 submesh / 0 child `Renderer*`
- เกม: 5 children `Renderer0-4`; asset ใน bundle: atlasAssets 1, materials 1
- สำรวจ 21*/11*: 350/741 ชื่อ, สถิติตามข้อ 7
- `dbbodyconfig`: 2110101 → id 100011, 1006; `disciplebodyimage`: 178 แถวใช้ 100011

**Inferred**
- 21* = half-body portrait rig, 11* = full-body in-scene; หลักที่ 3 = ชุด/ไลบรารี ไม่ใช่หมวด использования
- 5 submesh ของเกมมาจากการรวม 4-5 atlas ของชิ้นส่วน (body/face/hair/hat/accessory) เข้ากับ rig 2110101
- skin ของ 2110101 ต้องถูกรวม (body+face+hair+hat) ไม่ใช่เลือกตัวเดียว จึงจะเห็นทั้งตัว

**Unknown**
- องค์ประกอบจริงของ SkeletonGraphic ฝั่งเกม (atlasAssets/MaterialCount/allowMultipleCanvasRenderers) → ต้องดูใน UnityExplorer
- ชื่อ skin ที่เกม set ให้ Alvis Baker
- แถว `disciplebodyimage` ของ Alvis (in_side/out_side) — ชื่อลูกศิษย์ไม่ปรากฏใน config ฝั่งไคลเอนต์
- เหตุผลที่ 2110101 มี region `2110101_2_1..2_3` ที่ไม่มี skin ไหนอ้างถึง (14 regions vs 11 skins)
