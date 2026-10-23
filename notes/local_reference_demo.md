# Local reference demo — Spine 5 slot จากเกม (scene char, portrait, chibi, third, weapon)

วันที่: 2026-10-23
โปรเจกต์: `C:\Workspace\REA\my_clone` (Unity 2021.3.45f2, color space = Gamma / `m_ActiveColorSpace: 0`)
เกม (อ่านอย่างเดียว): `C:\Workspace\REA\game_copy\Immortal Clan Grandmastery_Data`

ขอบเขต: ดึง Spine 5 ชุด (scene char `1113002_1`, portrait `2110101`, chibi `1123011`, third `1113101_1`, weapon) มาแสดงในซีนเดียวเพื่อดูภาพรวม
รายละเอียดรอบพอร์ตเทรต (ไฟล์/สกิน/ผลตรวจ) อยู่ใน `notes/portrait_2110101.md`
**ยังไม่**ประกอบหน้าตาให้ตรงกับตัวละครเฉพาะ (เช่น Amy Robinson) — ดูข้อ 8 ว่าต้องใช้อะไรเพิ่ม

ไฟล์จากเกมทั้งหมดอยู่ใน `my_clone/Assets/_LocalReference/` ซึ่งถูก ignore แล้วใน `.gitignore`
(`my_clone/Assets/_LocalReference/` และ `my_clone/Assets/_LocalReference.meta` ถูกเพิ่มก่อนคัดลอกไฟล์)
ไม่มีการคัดลอก DLL จากเกม และไม่ได้แก้ `SpineUiActor`

---

## 1. ไฟล์ที่ดึงมา (Observed)

สคริปต์: `extracted/extract_spine_demo.py` (UnityPy, อ่าน bundle อย่างเดียว ไม่เขียนทับเกม)

| ชุด | bundle ในเกม | sha256 ของ bundle | ไฟล์ที่ได้ |
|---|---|---|---|
| FullBody | `StreamingAssets\Windows\character\spinelib\body\1113002_1_skeletondata.ab` | `1e511064bbc7b841db204ccc0a0b5af77d04aae4c01d330d99e878febdc1fefd` | `FullBody/1113002_1.skel.bytes` (539,379 B), `1113002_1.atlas.txt` (2,007 B), `1113002_1.png` (364×80) |
| Chibi | `StreamingAssets\Windows\character\spinelib\body_npc\1123011_skeletondata.ab` | `cf45fb5eb81088b7cd2e20bf6338b9881a27bcb0c25593e402003a8c25cef1da` | `Chibi/1123011.skel.bytes` (18,265 B), `1123011.atlas.txt` (8,673 B), `1123011.png` (1016×272) |
| Third | `StreamingAssets\Windows\character\spinelib\body\1113101_1_skeletondata.ab` | `acce54075a05ea2f305789fcb4c487ee3db0fac2b9b234accad681697995565f` | `Third/1113101_1.skel.bytes` (194,142 B), `1113101_1.atlas.txt` (2,261 B), `1113101_1.png` (128×392) |
| Weapon (component) | `StreamingAssets\Windows\character\spinelib\weapon\weapon_skeletondata.ab` | `04ade5c055d1d38965efb0f1c1075ce77872f8c4f2c0f72abbed2eb7bd1a855c` | `Weapon/weapon.skel.bytes` (12,445 B), `weapon.atlas.txt` (14,708 B), `weapon.png` (2048×364) |

- ทั้งสอง bundle มี atlas **1 หน้า** → texture 1 ไฟล์ต่อชุด (ชื่อไฟล์ตรงกับ page line ใน atlas)
- ชุดที่สาม (`Third`) มาจาก bundle `body/1113101_1` (สกัดไว้ก่อนหน้าที่ `%TEMP%\spine_cand\body1113101`) — คัดลอกเข้า `Assets/_LocalReference/Third/`
- `source.json` ในแต่ละโฟลเดอร์เก็บ bundle path, sha256, รายละเอียด material ที่อ่านได้จาก bundle (ใช้ตรวจตอน import)
- ไม่ต้องหาชื่อ bundle ใกล้เคียง: ชื่อที่ใช้ตรงกับที่ระบุ (full body = `body\1113002_1`, chibi = `body_npc\1123011`)

---

## 2. ข้อมูลในไฟล์ (Observed — parse ด้วย runtime 3.8 ของโปรเจกต์)

| | FullBody `1113002_1` | Chibi `1123011` |
|---|---|---|
| Spine version | **3.8.75** | **3.8.75** |
| SkeletonDataAsset.scale ที่เกมใช้ | 0.01 | 0.01 |
| bones / slots / skins / animations | 77 / 25 / 4 / 24 | 25 / 15 / 15 / 6 |
| setup size (W×H, unit) | 93.50 × 69.76 | 95.70 × 73.05 |

Skins:
- FullBody: `default`, `body_1113002_1`, `face_1113002_1`, `head_1113002_1`
- Chibi: `default`, `body_1123011_1` … `body_1123011_7`, `head_1123011_1` … `head_1123011_7`

Animations (FullBody, 24):
`attack1` (0.967s), `attack2` (1.067s), `attack3` (1.1s), `attack4` (1.1s), `attack5` (1.3s),
`attack6` (4s), `attack7_1` (0.333s), `attack7_2` (2.667s), `attack7_3` (3.9s), `attack8` (1.133s),
`attack9` (2.133s), `dead` (0.5s), `hit` (0.5s), `idle1` (0.6s), `idle2` (1s), `idle3` (0.667s),
`idle4` (1.267s), `idle5` (1.333s), `jump1` (0.133s), `jump2` (0.4s), `jump3` (0.3s),
`run` (1.067s), `stand` (2.667s), `walk` (0.8s)

Animations (Chibi, 6):
`idle1` (2.667s), `jump1` (0.1s), `jump2` (0.4s), `jump3` (0.333s), `run` (0.4s), `stand` (1.067s)

Slots ของ FullBody: `youshou w_jian w_baozhu w_fan w_zhang w_maobi dimao wei youmao youjiao zuojiao body daiz zuoshou zuomao hair_f5 youer head face hair_f4 zuoer hair_f3 hair_f2 hair_f1 root_boundingBox`
→ 5 slot อาวุธ (`w_jian`, `w_baozhu`, `w_fan`, `w_zhang`, `w_maobi`) ว่างในไฟล์นี้ (ไม่มี attachment ใน skin ใด)

---

## 3. การนำเข้า (Observed)

`Assets/Editor/LocalReferenceDemoBuilder.cs` — เมนู **Tools/Local Reference/Build Demo Scene**
หรือ batch: `-executeMethod MyClone.EditorTools.LocalReferenceDemoBuilder.BuildBatch`

สิ่งที่สคริปต์ทำ:
1. texture import: `TextureType = Default`, `alphaIsTransparency = false`, `mipmapEnabled = false`,
   `sRGBTexture = true`, `wrapMode = Clamp`, `filterMode = Bilinear`, `nPOTScale = None`,
   `textureCompression = Uncompressed`, `maxTextureSize = 2048` (ตรวจใน .meta แล้ว: `alphaIsTransparency: 0`, `nPOTScale: 0`, `sRGBTexture: 1`)
2. สร้าง material `Spine/SkeletonGraphic`, ผูก texture, ตั้ง `_StraightAlphaInput = 0` และปิด keyword `_STRAIGHT_ALPHA_INPUT`
   (ตรงกับ material ในเกม: `_StraightAlphaInput: 0.0`)
3. สร้าง `SpineAtlasAsset` จาก `.atlas.txt` + material (`SpineAtlasAsset.CreateRuntimeInstance`)
4. สร้าง `SkeletonDataAsset` (`CreateRuntimeInstance`, scale = 0.01, defaultMix = 0.2) แล้ว `GetSkeletonData(true)`
   เพื่อบังคับ parse ทันที (ถ้า parse ไม่ผ่านจะ throw พร้อมชี้ไปที่ guard 3.8.75 ใน `SkeletonBinary.cs`)
5. ไม่ได้พึ่ง Spine auto-import: เขียน asset เองด้วย Editor API ทั้งหมด (ไม่ต้องกด Import ใน Inspector)
6. ลบ `Assets/New SkeletonDataAsset.asset` ที่ root ถ้าไม่มีใครอ้างถึง → ตรวจแล้วไม่มี asset ใดอ้าง (ลบแล้วในรอบนี้)

ไฟล์ที่สร้าง:
```
Assets/_LocalReference/FullBody/1113002_1_SkeletonData.asset
Assets/_LocalReference/FullBody/1113002_1_Atlas.asset
Assets/_LocalReference/FullBody/1113002_1_Material.mat
Assets/_LocalReference/FullBody/1113002_1_Material_Gray.mat
Assets/_LocalReference/Chibi/1123011_SkeletonData.asset
Assets/_LocalReference/Chibi/1123011_Atlas.asset
Assets/_LocalReference/Chibi/1123011_Material.mat
Assets/_LocalReference/Chibi/1123011_Material_Gray.mat
Assets/_LocalReference/Third/1113101_1_SkeletonData.asset
Assets/_LocalReference/Third/1113101_1_Atlas.asset
Assets/_LocalReference/Third/1113101_1_Material.mat
Assets/_LocalReference/Third/1113101_1_Material_Gray.mat
Assets/_LocalReference/Demo.unity
```

---

## 4. ซีน Demo (Observed — build 2026-10-23 รอบล่าสุด, 5 slot: 3 ตัวละคร + weapon + portrait ที่ใช้ slot เดียวกับ FullBody)

`Assets/_LocalReference/Demo.unity` สร้างด้วย **Tools/Local Reference/Build Demo Scene**

- **เปิดซีนเดิมถ้ามี** (`OpenScene`) → เก็บ `Main Camera` เดิมไว้ (log: `kept existing camera 'Main Camera'`)
  แล้วลบ root object อื่นทั้งหมดก่อนสร้างใหม่; ถ้าไม่มีซีนจะสร้าง EmptyScene + camera เอง
- Camera 1 ตัว: SolidColor (0.07, 0.08, 0.11), orthographic size 5, z = −10, tag `MainCamera`
- EventSystem 1 ตัว (StandaloneInputModule)
- Canvas: Screen Space - Overlay + CanvasScaler `ScaleWithScreenSize`, reference 1920×1080, match 0.5
- ใต้ Canvas: `ActorRoot` (RectTransform แบบ stretch เต็มจอ) ถือ actor ทั้งห้า (FullBody / Chibi / Third / Weapon / Portrait) + `DemoCanvasLayout` + `DemoZoomPan`;
  UI (Title / InfoPanel / Controls) อยู่ใต้ Canvas โดยตรง → ซูม/แพนไม่กระทบ UI

| | `FullBody_1113002_1` | `Chibi_1123011` | `Third_1113101_1` | `Weapon_w_jian` |
|---|---|---|---|---|
| ตำแหน่ง (สัดส่วนความกว้าง canvas) | −0.25 (ซีกซ้าย) | +0.20 | +0.40 | **+0.06** (ระหว่าง FullBody กับ Chibi) |
| ความสูงเป้าหมาย | **70%** = 756 px | **25%** = 270 px | **25%** = 270 px | **25%** = 270 px |
| mesh ที่วัด (setup ∪ idle 0.1s) | 115.254 × 110.548 | 120.699 × 84.860 | 125.455 × 154.416 | 70.6 × 10.8 (**หมุน 90°**) |
| localScale | 6.633 | 3.086 | 1.696 | 3.71 |
| ขนาดบนจอ (fit) | 764.5 × 756 | 372.5 × 270 | 212.8 × 270 | 40 × 262 (แนวดิ่ง) |
| ฐานเท้า | y = −400 (140 px จากขอบล่าง) | เท่ากัน | เท่ากัน | เท่ากัน |
| Initial Skin | `body_1113002_1` | `body_1123011_1` | `body_1113101_1` | `w_jian/w_jian_300101` (skin เดียวกับปุ่ม Mount) |
| Initial Animation (loop) | `idle1` | `stand` | `idle1` | `stand` (animation เดียวของ weapon) |
| normalMaterial | `FullBody/1113002_1_Material.mat` | `Chibi/1123011_Material.mat` | `Third/1113101_1_Material.mat` | `Weapon/weapon_Material.mat` |
| grayMaterial | `..._Material_Gray.mat` (โฟลเดอร์เดียวกัน) | เหมือนกัน | เหมือนกัน | `Weapon/weapon_Material_Gray.mat` (สร้างใหม่รอบนี้) |

- **`Portrait_2110101`** (รอบล่าสุด) ใช้ **slot เดียวกันกับ FullBody** (−0.25, 70%, ฐานเท้าเดียวกัน) — แสดงทีละตัวด้วย Graphic alpha
  (ตัวที่ซ่อนยัง built อยู่ จึงยังวัด mesh/ปุ่มทำงานได้); mesh 656.0 × 613.1 → 784.7 × 756 px, Initial Skin `body_2110101_4_1`,
  animation `stand` (เดียวของ skeleton), material `Portrait/2110101_Material.mat`; เริ่มต้นซ่อน แล้วปุ่ม **Show Portrait** สลับ
  → ดูตัวเลข/ผลตรวจใน `notes/portrait_2110101.md`
- weapon เป็น attachment เดียว (4 verts, atlas region `300101` 71×11) — ตั้งฉาก 90° เพื่อให้ blade ยืนในช่องว่างโดยไม่คร่อมตัวละคร
  (mesh กว้าง ~6.5 เท่าของสูง; ถ้าวางแนวนอนที่ 25% จะกว้าง ~1,700 px)
- actor ทั้งสี่ถูกเก็บ reference ครบใน `SpineDemoController` (`fullBody`, `chibi`, `third`, `weapon`, `info`) + `mountData/mountSkin/mountBone` — ไม่มี None
  (weapon ไม่ถูก `Next Skin`/`Next Animation` แตะ — ตั้งใจให้เป็น reference ของ skin ที่ mount อยู่)
  (`initialLoop = 1`, `canvasGroupCompatible = 0` → pmaVertexColors = true)
- **ซูม/แพน**: `DemoZoomPan` บน `ActorRoot` — wheel = ซูมเข้าหาตำแหน่งเมาส์ (0.6×–8×), คลิกขวาลาก = แพน, ปุ่ม `Reset View` = กลับ 1×
  **คีย์บอร์ด**: `+`/`=`/`]` = ซูมเข้า (×1.25), `-`/`[` = ซูมออก, `0` = reset, ลูกศร = แพน (กด Shift = เร็ว 3 เท่า)
  **อ่านค่าซูม**: `Zoom 1.00x   pan 0, 0` มุมขวาบน (ใต้ Title) + บรรทัดคำสั่งคีย์ใต้มัน; ทั้งคู่เป็น UI นอก `ActorRoot` จึงไม่ถูกซูม
  (`Zoom`, `Pan`, `ZoomBy`, `ZoomIn`, `ZoomOut`, `PanBy`, `SetZoom`, `ResetView`, `Label` เป็น public;
  ตรวจใน batch แล้วว่า SetZoom(2.5) → scale 2.5, ZoomIn → 3.125, ZoomOut → 2.5, PanBy(40,−30) → pan (40,−30),
  Reset กลับ 1/pan 0 และ readout แสดงค่าตรงทุกขั้น)
- **ทดสอบหลายอัตราส่วน (Observed)**: `DemoCanvasLayout` ใช้ mesh box ที่ builder วัดไว้ แล้วแมปกับ canvas rect สด
  ⇒ 16:9 (1920×1080), 4:3 (1663×1247), 21:9 (2217×935) ให้ผลเท่ากัน: **67.9% / 24.3% / 24.3% / 24.3%** ของความสูง canvas
  และตำแหน่ง mesh center x = **−480 / 384 / 768 / 115.2**, **−415.8 / 332.6 / 665.2 / 99.8**, **−554.3 / 443.4 / 886.8 / 133** ตามลำดับ
  (ค่า = `heightFraction × 0.97` ของความสูง; 0.97 คือ margin กัน bbox ที่โตตาม idle ไปทับแถบปุ่ม)
- **Ultrawide (Observed)**: `topReserve = 88` (แถบ Title) — ที่ **32:9 (2715×764)** ไม่ต้อง clamp (top 646.8 < 676) ยังได้ 67.9/24.3/24.3
  และ `InfoPanel` ย้ายจากมุมซ้ายล่างไป**มุมขวาบน** (ใต้ readout/hint, `infoDockTop = 168`) เพราะ aspect 3.55 ≥ `infoDockAspect = 2.4`
  → ไม่ทับ actor ใดเลย (`infoPanel=right overlap=False` ทั้ง 4 slot); ที่ **48:9 (3325×623)** FullBody ถูก clamp ใต้แถบ Title
  เหลือ **65.3%** (top = 535 = stageTop พอดี) ส่วน Chibi/Third/Weapon ยัง 24.3% — ไม่มี overlap (stress case); weapon x = 163 / 200 ที่ 32:9 / 48:9
  21:9 (2.37) ยังไม่ถึงเกณฑ์ → panel อยู่มุมซ้ายล่างเหมือนเดิม
- `DemoCanvasLayout` ตอบ `OnRectTransformDimensionsChange` ด้วย → ตอน `SaveScreenshot` ที่สลับ canvas เป็น Screen Space - Camera
  และตอนผู้เล่นเปลี่ยนขนาดหน้าต่าง จะจัดวางใหม่ในเฟรมเดียวกันก่อนเรนเดอร์
- Info: panel สีเข้ม 980×176 + Text 18 px (1 บรรทัดต่อ actor + บรรทัด `shown` + mounted) แสดง bones/slots/skins/anims + skin/animation
  ของ Scene char / Portrait / Chibi / Third (weapon ไม่มีบรรทัดในแผง — ชื่อ `w_jian` อยู่ใน Title)
  ตำแหน่งปกติ (24, 118) ซ้ายล่าง; canvas กว้าง/สูง ≥ 1.7 จะ dock มุมขวาบนที่ (−24, −164) ให้พ้นแนว actor
  (16:9 จึง dock ด้วย; 4:3 ยังซ้ายล่างเพราะหัว actor จะอยู่ใต้แผง)
- ปุ่ม 9 ปุ่มใต้ canvas (panel 1740×84 ที่ y = 20, ปุ่ม 180×56): `Show Portrait` / `Show Scene Char`, `Next Skin`, `Next Animation`,
  `Flip X`, `Toggle Gray`, `Reset View`, `Mount`, `Audit Skins`, `Screenshot`
  ผูกด้วย `UnityEventTools.AddPersistentListener` (observed: `buttons=9`)

ผล `Verify` ท้าย build (edit mode, รอบล่าสุด): `cameras=1 canvases=1 buttons=9 errors=0`;
FullBody 745.2 px = 69% (fit 67.9%, meshX −477), Chibi 260.5 px = 24.1% (meshX 388), Third 246 px = 22.8% (meshX 769)

---

## 5. ผลการแสดง (Observed)

### 5.0 ผล `Audit Skins` (batch mode, 2026-10-23)

- FULL BODY: `groups=body/face/head variants=1 slots=25`
  `variant 0: skins=[body_1113002_1,face_1113002_1,head_1113002_1] slots=20/25 verts=486 mesh=115x105.4 core=ok missing=[w_jian,w_baozhu,w_fan,w_zhang,w_maobi]`
  → complete variants = `[0]` (slot ที่ว่างคือช่องอาวุธ ซึ่งถูกเติมด้วย skin จาก `weapon/weapon_skeletondata.ab` เมื่อมีอาวุธ)
- CHIBI: `groups=body/head variants=7 slots=15` — ทุก variant ปิดครบ `slots=15/15` แต่ mesh ต่างกัน:
  `1→118.1x85`, `2→135.4x106.4`, `3→118.3x88`, `4→122.3x91.5`, `5→118.2x96.2`, `6→126.9x107.2`, `7→114.4x98.8`
  → complete variants = `[0..6]` ตามเกณฑ์ slot
  แต่ **`dbbodyconfig` ประกาศคู่ที่ใช้จริงแค่ variant 1–5** (id 2055–2059) — 6/7 มี skin อยู่แต่ไม่มี body row คู่ใน bundle นี้
  (ดู `notes/character_bundle_mapping.md` ข้อ 1)

### 5.1 การ mount component (sub-skeleton)

- `Mount` สำเร็จ: `mounted weapon_SkeletonData (skin w_jian/w_jian_300101) at bone 'wuqi' ok=True`
- จำนวน `SkeletonGraphic` ใต้ full body = **3** ใน editor (body + mount + ตัวเก่าที่รอ destroy), ใน play mode = 2
- sword ขึ้นจริงจาก texture 2048×364 ของมันเอง (เห็นในภาพเรนเดอร์) แต่ **วางแนวนอนพาดหน้า** เพราะ
  `SpineUiActor.MountToBone()` ตั้ง `followBoneRotation = false` และ bone `wuqi` ไม่มี transform ของ attachment
  → การ mount แบบ sub-skeleton เหมาะกับของที่เกมตั้ง `cmpRootBone` ไว้ แต่ **อาวุธในเกมใช้วิธี merge skin** ไม่ใช่ mount
- ข้อสำคัญ: component ที่มี `followSlot` จริง (311 แถวใน `dbcomponentcfg`) **ไม่มี bundle ใดในเครื่องนี้เลย**
  → ทดสอบด้วย weapon skeleton แทน; เมื่อมีชุด bundle ครบสามารถเปลี่ยน `mountData` เป็น component นั้นได้ทันที

### 5.2 ผลการรันจริงใน Play mode (batch, 2026-10-23, 3 actor)

`PlayCheckBatch` เปิด `Demo.unity` → `EditorApplication.EnterPlaymode()` → รอ 20 เฟรม → ตรวจ → ออกด้วย exit code
(รอบล่าสุด: **PASS, exit 0, errors = 0**; ใช้ `-batchmode` ไม่ใส่ `-nographics`/`-quit`)

- canvas ใน play ของ batch สูง **1247 หน่วย** (4:3) — ตรวจโดยใช้ความสูงจริง ไม่ใช้ค่าคงที่ 1080
- FULL BODY: `verts=486 shader=Spine/SkeletonGraphic skin=demo_combined_0 animation=idle1` → **861.9 px = 69.1%** (scale 7.66)
- CHIBI: `verts=126 ... animation=stand` → **300.8 px = 24.1%** (scale 3.564)
- THIRD: `verts=359 ... animation=idle1` → **283.3 px = 22.7%** (scale 1.958)
- WEAPON: `verts=4 shader=Spine/SkeletonGraphic skin=w_jian/w_jian_300101 animation=stand` → **302 px = 24.3%** (scale 4.282, หมุน 90°)
- `DemoCanvasLayout.ApplyForSize(1663×1247)` ระหว่างเล่น → 67.9% / 24.3% / 24.3% / 24.3% และ x = −415.8 / 332.6 / 665.2 / 99.8 ตามที่ออกแบบ
  แล้วคืนค่าเป็นขนาด canvas จริงทันที (เทสต์เดียวกับที่ `Update` ใช้)
- edit-mode `Verify` ตรวจเพิ่ม 32:9 (2715×764) และ 48:9 (3325×623): ไม่มี actor ไหนล้ำแถบ Title และไม่ทับ Info panel ที่ dock ขวา
  (48:9 FullBody ถูก clamp เหลือ 65.3%; 32:9 ยังได้ 67.9%) — ดูข้อ 4
- readout ระหว่างเล่น: `Zoom 2.50x   pan 40, -30` (หลัง SetZoom 2.5 + PanBy) → `Zoom 1.00x   pan 0, 0` หลัง ResetView
  (ปุ่ม/คีย์เรียกเมธอดเดียวกัน; batch เรียก `ZoomIn/ZoomOut/PanBy` ตรง ๆ เพราะจำลอง `Input.GetKey` ไม่ได้)
- ปุ่มที่กดผ่าน `onClick.Invoke()`: Next Skin / Next Animation / Toggle Gray / Flip X / Reset View — ไม่มี exception,
  verts รวมคงที่ 971 (นับเฉพาะ 3 ตัวละคร; weapon 4 verts ไม่ได้อยู่ในผลรวมนี้)
- ภาพเรนเดอร์ 1920×1080 (วิเคราะห์ pixel): FullBody x 114–840, Chibi+Third x 1169–1834, sword ~x 1,070 (ยืนแนวตั้งระหว่าง FullBody กับ Chibi) ไม่หลุดขอบ
- ภาพเก็บไว้: `Assets/_LocalReference/Screenshots/demo_four_slots.png` (รอบ 4 slot ล่าสุด — ใช้เป็นหลักฐาน),
  `demo_01_idle1_variant0.png` (ภาพจากปุ่ม Screenshot รอบเดียวกัน); `demo_three_actors.png` / `demo_built_idle1.png` / `demo_after_buttons.png` เป็นรอบก่อนหน้า

**Observed (ทำไมไม่ใช่ 70/25 เป๊ะ)**: fit ที่ 97% ของเป้าหมาย เพื่อให้ bbox ที่โตขึ้นตาม idle (สูงขึ้น ~2–5%) ไม่ล้นลงไปใต้แถบปุ่ม;
68–69% / 23–24% คือช่วงที่วัดได้จริงระหว่างเล่น

### 5.4 ผลการรันล่าสุด — 5 slot + ปุ่ม swap (Observed, 2026-10-23)

- canvas 1247 (4:3): `SCENE CHAR (fox)` verts 486 → **848.6 px = 68.0%** (scale 7.66); `PORTRAIT 2110101` verts 24 → **846.8 px = 67.9%** (scale 1.381)
- CHIBI 299.4 px = 24.0% (verts 126), THIRD 283.7 px = 22.7% (verts 359), WEAPON 4 verts = 24.3%
- `ApplyForSize(1663×1247)`: 67.9 / 24.3 / 24.3 / 24.3 / 67.9% และ x = −415.8 / 332.6 / 665.2 / 99.8 / **−415.8** (portrait ใช้ x เดียวกับ SCENE CHAR)
- ตอน `Start()`: portrait `alpha=0`, fox `alpha=1`; กด `Show Portrait` → portrait alpha 1 / fox alpha 0 / caption เปลี่ยนเป็น `Show Scene Char`
  กดซ้ำ (ผ่าน `ToggleMainActor`) → กลับเป็น fox และ caption กลับเป็น `Show Portrait`
- ปุ่มที่กด: Next Skin / Next Animation / Toggle Gray / Flip X / Reset View — ไม่มี exception; verts รวม **995** (486+126+359+24)
- `PORTRAIT renderers`: `SkeletonGraphic=1 CanvasRenderer=1 allowMultipleCanvasRenderers=False atlasPages=1 verts=24`,
  slots ที่วาด 6/19: `hair_b`, `body`, `face`, `face_acc`, `hair_f`, `hat_f` (หลัง Next Skin 2 ครั้ง — พิสูจน์ว่า `face` กับ `face_acc` อยู่ร่วมกันได้)
- ภาพ: `Screenshots/demo_five_slots.png` (ค่าเริ่มต้น = fox) และ `Screenshots/demo_portrait_2110101.png` (หลัง swap เป็น portrait)

---

### 5.3 กลไก batch play check (Observed)

- `EditorApplication.EnterPlaymode()` ใน `-executeMethod` แล้ว domain reload จะล้าง static ทั้งหมด → สถานะ "กำลังตรวจ" เก็บใน `EditorPrefs`
  และมี `[InitializeOnLoadMethod]` ลงทะเบียน `EditorApplication.update` ใหม่หลัง reload
- รอ `EditorApplication.isPlaying` แล้ว 20 เฟรม (ให้ `Start()` สร้าง actor + animation ได้ tick) → ตรวจ → `EditorApplication.Exit(0/2)`
- timeout 180 วินาที → exit 1 (กันค้างแบบรอบก่อนที่ต้อง kill process)
- ต้องมี graphics device (ไม่ใส่ `-nographics`) ไม่งั้น `SaveScreenshot` render ไม่ได้
- ค่าที่ใช้ตัดสิน: การมีตัวละคร/verts/material, skin+animation ที่เล่นจริง, ความสูงเทียบความสูง canvas จริง,
  `DemoCanvasLayout.ApplyForSize(4:3)` ระหว่างเล่น, `DemoZoomPan` set/zoom-in/zoom-out/pan/reset + readout ตรงกับค่าจริง,
  ปุ่มทั้ง 5 ทำงาน และมีไฟล์ภาพออกมา; ส่วน 32:9/48:9 เป็น edit-mode `CheckLayout` (คำนวณ canvas rect สมมติ
  เพราะ batch ไม่สามารถเปลี่ยนความละเอียด Game view ได้)

## 6. ปัญหาที่เหลือ (Observed / Inferred)

1. **Texture ตัวเต็มตัวละเอียดต่ำ (364×80) — ไม่มีชุดความละเอียดสูงในเครื่องนี้**
   - Observed: ทั้ง `StreamingAssets` มีไฟล์ที่ชื่อมี `1113002` เพียงไฟล์เดียว (`body/1113002_1_skeletondata.ab`) และ texture ในนั้น 364×80
   - Observed: `dbbodyconfig[1113002].size = [0.59, 0.91]` และ mesh ธรรมชาติ ~70×105 px → เกมแสดงตัวนี้ที่ขนาดเล็ก (~70–90 px)
     การขยายเป็น 756 px (70% canvas) ในเดโมจึงเบลอตามธรรมชาติ ไม่ใช่ปัญหาการ import
   - Observed: `s_spineAb` (`body_s/1113002_1_s`) และชุด `body_shizhuang/1113002_2/3` มีใน config แต่ **ไม่มีไฟล์ในเครื่อง**
   - Observed: ในเครื่องมี bundle ของ `character/spinelib` แค่ **30 จาก 4,463** ที่ config อ้างถึง
   - Unknown: ชุดติดตั้งเต็มมี body ความละเอียดสูงหรือไม่ (ต้องดาวน์โหลด bundle เพิ่ม)
2. **จำนวน renderer ไม่ตรงกับที่เห็นในเกม (5)**: ไฟล์ที่ดึงมามี atlas 1 หน้า → `SpineUiActor` จึงใช้ CanvasRenderer เดียว
   - Observed: หลังเพิ่ม weapon component แบบ mount ได้ 2 renderer (body + mount) → กลไก multi-renderer ทำงาน
   - Inferred: 5 renderer ในเกม = body + component ที่ `followSlot` ไม่ว่าง (แต่ละชิ้นเป็น sub-skeleton)
   - Observed: อาวุธ (`weapon/weapon_skeletondata.ab` + skin `w_jian/...`) ไม่มี `followSlot` → เกม merge เข้า slot `w_jian` ไม่เพิ่ม renderer
   - Unknown: component ตัวไหนที่เกมใช้กับ 1113002 จริง (bundle ยังไม่มาในเครื่อง)
3. ~~Play mode ยังไม่ได้รันจริงใน batch~~ → **แก้แล้ว (Observed)**: `PlayCheckBatch` เข้า play mode ใน batch ได้จริงบน 2021.3.45f2
   (ต้องไม่ใส่ `-nographics`, ไม่ใส่ `-quit`, เก็บสถานะข้าม domain reload ด้วย `EditorPrefs` + `[InitializeOnLoadMethod]`; timeout 180 วินาที)
   รอบล่าสุด PASS exit 0 — รายละเอียดที่ตรวจดูข้อ 5.2/5.3
4. **Actor ถูกสร้างใหม่ทุกครั้งที่โหลดซีน**: `Start()` จะ `Build()` ใหม่ (destroy/recreate `SpineGraphic`)
   - Observed: ซีนที่บันทึกไว้มี `SpineGraphic` ของรอบ editor build ค้างอยู่ (verts=0) เพราะ `SpineUiActor` ไม่ serialize renderer reference
     → `SpineDemoController.EnsureBuilt()` ลบ child ที่ค้างก่อน `Build()` (แก้เฉพาะ demo ไม่แตะ `SpineUiActor`); หลังแก้ตรวจได้
     `SkeletonGraphic=1 CanvasRenderer=1` ต่อ actor
   - ใน play mode ก่อนหน้านี้ไม่เห็นปัญหาเพราะ child ที่ค้างมี 0 verts (ไม่วาด)
5. **คำเตือนตอน AddComponent**: ตามข้อ 5 (แก้ไม่ได้ถ้าไม่แก้ `SpineUiActor` ซึ่งห้ามแก้ในรอบนี้)
6. **Weapon slot แสดง skin เดียว**: `weapon` skeleton มี 153 skins แต่ slot ที่สี่เล่นเฉพาะ `w_jian/w_jian_300101`
   (skin เดียวกับที่ปุ่ม Mount ใช้) และ mesh เป็น quad เดียว (4 verts) — skin อื่น (ดาบ/กระบี่คนละแบบ) ไม่มีปุ่มให้วน
   - Observed: วางแนวนอนที่ 25% จะกว้าง ~1,715 px (aspect ~6.5:1) จึงหมุน 90° และใช้ `maxWidthFraction` เป็นเพดานกันพลาด
   - Observed: ที่ 4:3 sword อยู่ x 77–123 ซึ่งทับพื้นหลัง Info panel บางส่วน (เหมือนขา FullBody ที่ทับอยู่แล้ว);
     เฉพาะ ultrawide ที่ `CheckLayout` บังคับว่า overlap = False

---

## 7. วิธีใช้

### 7.1 เปิดดู

1. เปิด Unity Editor ที่โปรเจกต์ `my_clone`
2. เปิด `Assets/_LocalReference/Demo.unity` (**ไม่ใช่ซีน Untitled ที่ค้างอยู่** — ถ้ามีซีน Untitled ให้ปิดทิ้งโดยไม่ต้องบันทึก)
3. กด Play: `SpineDemoController.Start()` จะ `Build()` actor ทั้งห้าใหม่ (เพราะ `SpineUiActor.m_Graphic` ไม่ถูก serialize),
   รวม skin กลุ่มละตัว แล้วเล่น `InitialAnimation` ที่เซ็ตไว้ (`idle1` / `stand` / `stand` / `idle1` / `stand`) — ป้าย Info แสดง
   bones/slots/skins/anims + skin/animation ของ Scene char / Portrait / Chibi / Third; ตัวที่ห้าคือดาบ `w_jian` แนวนอน→หมุน 90° ยืนระหว่าง SCENE CHAR กับ Chibi
   (ไม่ถูก `Next Skin`/`Next Animation` เปลี่ยน — ใช้เทียบกับชิ้นที่ปุ่ม Mount ติดตั้ง)
   ปุ่ม **Show Portrait** สลับช่อง 70% ซ้ายระหว่าง SCENE CHAR (fox) กับ PORTRAIT 2110101 (เริ่มที่ fox)
4. **ซูม/แพน**: หมุนล้อเมาส์ = ซูมเข้าหาเคอร์เซอร์ (0.6×–8×), คลิกขวาลาก = แพน, ปุ่ม `Reset View` = กลับ 1×
   **แป้นพิมพ์**: `+`/`=` = ซูมเข้า, `-` = ซูมออก, `0` = Reset View, ลูกศร = แพน (Shift = 3×)
   **มุมขวาบน** แสดง `Zoom 1.00x   pan 0, 0` (อัปเดตทันที) พร้อมบรรทัดคำสั่งคีย์
   (actor อยู่ใต้ `ActorRoot` ที่ถูกซูม/แพน; Title/Info/readout/ปุ่มไม่ขยับ) — ใช้ดู texture ของ 1113002_1 (364×80) แบบใกล้ ๆ ได้
5. ปรับขนาดหน้าต่าง/สลับ aspect ได้ระหว่างเล่น: `DemoCanvasLayout` คำนวณใหม่ทันที (ไม่ต้องกด Play ใหม่)
   — จอตั้งแต่ 16:9 ขึ้นไป Info panel ย้ายไปมุมขวาบนเอง; จอเตี้ยมาก actor สูงสุดจะถูกย่อให้พ้นแถบ Title

### 7.2 ปุ่ม

| ปุ่ม | เรียก | ผลที่เห็น |
|---|---|---|
| **Show Portrait** | `SpineDemoController.ToggleMainActor()` | สลับ Graphic alpha ระหว่าง SCENE CHAR (fox) กับ PORTRAIT 2110101 ที่ใช้ slot เดียวกัน; caption เปลี่ยนเป็น `Show Scene Char` เมื่อแสดง portrait |
| **Next Skin** | `SpineDemoController.NextSkin()` | เลื่อน variant ถัดไปของทุกกลุ่ม skin พร้อมกัน ทั้ง fox/portrait/chibi/third (fox: `body/face/head_1113002_1`; portrait: `body/face/face_acc/hair/hat_2110101_*`; Chibi: `body/head_1123011_1..7`; Third: `body/face/head_1113101_1`) แล้ว refresh ป้าย Info |
| **Next Animation** | `NextAnimation()` | เล่นแอนิเมชันถัดไปในรายการของ skeleton (คนละรายการระหว่าง actor) แบบ loop; portrait มี animation เดียว (`stand`) จึงไม่เปลี่ยน |
| **Flip X** | `ToggleFlip()` | `SpineUiActor.SetFlipX(!FlipX)` ทุกตัว (กลับด้านซ้าย–ขวา) |
| **Toggle Gray** | `ToggleGray()` | สลับ material ปกติ ↔ `Spine/SkeletonGraphic Grayscale` (`_GrayAmount = 1`) ให้ทุกตัวรวม portrait; กดอีกครั้งคืนสี |
| **Reset View** | `DemoZoomPan.ResetView()` | คืน zoom = 1 และ pan = 0 (เหมือนตอนเปิดซีน) |
| Mount | `ToggleMount()` | `fullBody.Mount(weaponData, "w_jian/w_jian_300101")` + `MountToBone("wuqi")`; กดอีกครั้ง = `UnMount()` (ซ่อน ไม่ลบ) |
| Audit Skins | `AuditSkins()` | log ทุก skin variant: slots ที่มี attachment, verts, ขนาด mesh, slot ที่ว่าง, สรุป `complete variants` |
| Screenshot | `CaptureScreenshot()` | render canvas 1920×1080 ลง `Assets/_LocalReference/Screenshots/demo_NN_<anim>_variant<k>.png` |

- **แป้นพิมพ์** (ไม่ใช่ปุ่ม): `+`/`=`/`]` = `DemoZoomPan.ZoomIn()`, `-`/`[` = `ZoomOut()`, `0` = `ResetView()`,
  ลูกศร = `PanBy()` ต่อเนื่อง (600 หน่วย/วินาที, Shift = 3×)
- **แถบอ่านค่า** มุมขวาบน (นอก `ActorRoot`): `Zoom 1.00x   pan 0, 0` + `+ / - = zoom   arrows = pan (shift = faster)   0 = reset`
  — เห็นใน `Screenshots/demo_01_idle1_variant0.png` (ภาพจากปุ่ม Screenshot รอบล่าสุด)

### 7.3 สร้างซีนใหม่

เมนู **Tools/Local Reference/Build Demo Scene** (ถ้าซีนปัจจุบันถูกแก้ จะถามให้บันทึกก่อน)
Builder จะเปิด `Demo.unity` เดิม เก็บ `Main Camera`, ลบ object อื่น, สร้าง Canvas/EventSystem/actor/ปุ่มใหม่,
รัน `Verify` แล้ว log `errors=0` (dialog แสดงจำนวน error ให้เห็นด้วย), จากนั้นบันทึกซีน

Batch (ใช้ path ของ Editor 2021.3.45f2):

```
Unity.exe -batchmode -projectPath <project> ^
  -executeMethod MyClone.EditorTools.LocalReferenceDemoBuilder.BuildBatch -logFile build.log

Unity.exe -batchmode -projectPath <project> ^
  -executeMethod MyClone.EditorTools.LocalReferenceDemoBuilder.PlayCheckBatch -logFile play.log
```

- `BuildBatch`: exit 0 เมื่อ build + verify ผ่าน, exit 1 เมื่อ error
- `PlayCheckBatch`: exit 0 = PASS, 2 = ตรวจไม่ผ่าน (log `[LocalReferenceDemo][play]`), 1 = timeout/เข้า play mode ไม่ได้
- ห้ามใส่ `-nographics` (Screenshot ต้องใช้ graphics device) และ `-quit` (จะออกก่อนตรวจเสร็จ)

## 8. ถ้าจะประกอบหน้าตาให้ตรงกับตัวละครเฉพาะ (เช่น Amy Robinson) ต้องใช้อะไรเพิ่ม

**ได้มาแล้วในรอบนี้**: กลไก map id → bundle ครบ (ดู `notes/character_bundle_mapping.md`)

| ข้อมูล | ตาราง/file | สถานะ |
|---|---|---|
| body id → bundle + skin ของตัว | `dbbodyconfig` (4,574 แถว) + `config/rawdata/dbbodyconfig` | ✅ parse แล้ว (`extracted/dbbodyconfig.json`) |
| ชิ้นส่วน/อาวุธ/หมวก → bundle + skin + จุดติดตั้ง | `dbcomponentcfg` (6,174 แถว) | ✅ parse แล้ว (`extracted/dbcomponentcfg.json`) |
| วิธีรวม skin / mount sub-skeleton | `ActionSystem/SpineActor.cs` (240–300, 493–560, 2113–2128) | ✅ อ่านแล้ว |
| **appearance config ที่ให้ `bodyID` + `slots[]`** | `npcimageconfig` / `discipleconfig.imagelib` → `disciplebodyimage` → `disciple{hair,face,faceaccessory,weapon}imageconfig` | ✅ พบ + มีเครื่องมือ `extracted/resolve_character_model.py` (ดู `notes/character_bundle_mapping.md` ข้อ 6) |

**ยังขาด (Unknown — ต้องไปเก็บจากเกม)**:
1. **id จริงของ NPC/disciple ที่จะประกอบ** — มาจาก npc config/ข้อมูลเซิร์ฟเวอร์ ไม่ได้อยู่ในตาราง appearance
2. **`clothingId` ที่ผู้เล่นสวมจริง** (คีย์ใน `clothing_out_side`) มาจากระบบ clothing/ไอเทม
3. **sorting order / offset ต่อชิ้นส่วน** — `jsonData.useSrcOffset` มีใน `dbcomponentcfg` แล้ว แต่ค่าจริงขึ้นกับแต่ละชิ้น
4. **bundle ที่ยังไม่ดาวน์โหลด** — ในเครื่องมี 30/4,463 body bundle และ 0/311 component ที่มี `followSlot`
   (มีผลกับทั้งชุดความละเอียดสูงและ multi-renderer ที่ใช้ข้อมูลจริง)

เมื่อได้ข้อ 1–2 จะประกอบ prefab ได้ด้วย `SpineActorFactory.CreateUi` + รวม skin (แบบ `ApplyCombinedSkin`)
+ `Mount/MountToBone` สำหรับชิ้นส่วนที่ `followSlot != ""`
