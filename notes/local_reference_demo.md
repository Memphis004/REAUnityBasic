# Local reference demo — Spine 2 ตัวจากเกม (full body + chibi)

วันที่: 2026-10-23
โปรเจกต์: `C:\Workspace\REA\my_clone` (Unity 2021.3.45f2, color space = Gamma / `m_ActiveColorSpace: 0`)
เกม (อ่านอย่างเดียว): `C:\Workspace\REA\game_copy\Immortal Clan Grandmastery_Data`

ขอบเขต: ดึง Spine ตัวเต็มตัว 1 ตัว + ตัวจิบิ 1 ตัว มาแสดงในซีนเดียวเพื่อดูภาพรวม
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
| Weapon (component) | `StreamingAssets\Windows\character\spinelib\weapon\weapon_skeletondata.ab` | `04ade5c055d1d38965efb0f1c1075ce77872f8c4f2c0f72abbed2eb7bd1a855c` | `Weapon/weapon.skel.bytes` (12,445 B), `weapon.atlas.txt` (14,708 B), `weapon.png` (2048×364) |

- ทั้งสอง bundle มี atlas **1 หน้า** → texture 1 ไฟล์ต่อชุด (ชื่อไฟล์ตรงกับ page line ใน atlas)
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
Assets/_LocalReference/Demo.unity
```

---

## 4. ซีน Demo (Observed — build 2026-10-23 รอบล่าสุด)

`Assets/_LocalReference/Demo.unity` สร้างด้วย **Tools/Local Reference/Build Demo Scene**

- **เปิดซีนเดิมถ้ามี** (`OpenScene`) → เก็บ `Main Camera` เดิมไว้ (log: `kept existing camera 'Main Camera'`)
  แล้วลบ root object อื่นทั้งหมดก่อนสร้างใหม่; ถ้าไม่มีซีนจะสร้าง EmptyScene + camera เอง
  (กันสภาพ "ซีน Untitled ที่มี object ค้าง" หลัง build ล้มกลางทาง)
- Camera 1 ตัว: SolidColor (0.07, 0.08, 0.11), orthographic size 5, z = −10, tag `MainCamera`
- EventSystem 1 ตัว (StandaloneInputModule)
- Canvas: Screen Space - Overlay + CanvasScaler `ScaleWithScreenSize`, reference 1920×1080, match 0.5

ผัง (reference pixel; 1 px = 1 หน่วย canvas):

| | `FullBody_1113002_1` | `Chibi_1123011` |
|---|---|---|
| ตำแหน่ง mesh center (x) | −480 (ซีกซ้าย) | +480 (ซีกขวา) |
| ความสูงเป้าหมาย | **70%** ของ canvas = 756 px | **25%** = 270 px |
| mesh ที่วัด (setup ∪ idle 0.1s) | 115.3 × 110.5 px | 135.4 × 87.7 px |
| localScale | 6.633 | 2.987 |
| ขนาดบนจอ (fit) | 764.5 × 756 px | 404.4 × 270 px |
| ฐานเท้า | y = −400 (140 px จากขอบล่าง) | y = −400 (เท่ากัน) |
| Initial Skin | `body_1113002_1` | `body_1123011_1` |
| Initial Animation (loop) | `idle1` | `stand` |
| normalMaterial | `FullBody/1113002_1_Material.mat` | `Chibi/1123011_Material.mat` |
| grayMaterial | `FullBody/1113002_1_Material_Gray.mat` | `Chibi/1123011_Material_Gray.mat` |

- Initial Skin = **skin แรกที่ไม่ใช่ `default`** (ทั้งสองตัวมี จึงไม่มีเคส "ปล่อยว่าง"; ถ้าไม่มี builder จะปล่อยว่างและ log `(none - no non-default skin)`)
  - skin เดียวไม่พอต่อการแสดงเต็มตัว (FullBody แยก `body_/face_/head_`) → builder และ `SpineDemoController` ยังรวม skin กลุ่มละตัวเป็น `demo_combined_N` ให้เห็นทั้งตัว
- การ fit: `scale = เป้าหมาย × 0.97 ÷ ความสูง mesh` โดยวัด mesh จริงหลัง `UpdateMesh()` (ขนาดจาก `SkeletonData.Height` ใช้ไม่ได้ — เป็น setup pose)
  - วัด union ของ setup pose (หลัง `SetSlotsToSetupPose`) กับ idle ที่ t = 0.1s เพราะ bbox ระหว่างเล่นโตขึ้นอีก ~2–5%
    ถ้า fit ที่ 100% ตรง ๆ เท้าจะจมลงไปใต้แถบปุ่ม (พบในรอบก่อน: 74% และเท้าอยู่ที่ 70 px จากขอบล่าง)
  - ชดเชย mesh center: `anchoredPosition = เป้าหมาย − meshCenter × scale` และ `sizeDelta` = ขนาด mesh (ให้ Canvas culling เห็น)
- **การเลือก loop (Observed)**: `idle1` ของ Chibi เป็นแอนิเมชันกลิ้ง → bbox กว้างขึ้น **8.73 เท่า** (เดินออกข้าง ~1,031 หน่วย)
  builder จึงตรวจ loop ทุกตัว (`growth ≤ 1.6×1.7`) แล้วเลือก `stand` (growth 1.29×1.10) ให้ Chibi;
  FullBody `idle1` ผ่าน (growth 1.02×1.09); `SpineDemoController.Start()` เล่น `InitialAnimation` ที่เซ็ตไว้ (ไม่ override กลับเป็น idle1)
- Material (Observed ในไฟล์ `.mat`): ปกติ `Spine/SkeletonGraphic` + `_StraightAlphaInput: 0` + keyword `_STRAIGHT_ALPHA_INPUT` ปิด (PMA)
  สีเทา `Spine/SkeletonGraphic Grayscale` (shader ใน `Assets/Scripts/SpineSystem/Shaders/Spine-SkeletonGraphic-Grayscale.shader`) + `_GrayAmount: 1` + texture เดียวกัน + PMA เหมือนกัน
  - `SpineUiActor.normalMaterial/grayMaterial` เป็น `private [SerializeField]` ไม่มี setter → builder เซ็ตด้วย `SerializedObject` (observed: ค่าในซีนไม่เป็น None, shader ตรงตามที่ตรวจ)
- Info: panel สีเข้ม 900×124 ที่ (24, 118) + Text 22 px ข้างใน — กันตัวอักษรขาวทับชุดของ FullBody
- ปุ่ม 7 ปุ่มใต้ canvas (panel 1610×84 ที่ y = 20): `Next Skin`, `Next Animation`, `Flip X`, `Toggle Gray`, `Mount`, `Audit Skins`, `Screenshot`
  ผูกด้วย `UnityEventTools.AddPersistentListener` (observed: `buttons=7` และทุกปุ่มมี persistent listener 1 ตัว)

Reference ของ `SpineDemoController` (อ่านจาก Demo.unity): `fullBody → FullBody_1113002_1`, `chibi → Chibi_1123011`,
`info → Info (Text)`, `mountData → weapon_SkeletonData`, `mountSkin = w_jian/w_jian_300101`, `mountBone = wuqi` — **ไม่มี None**
(`initialLoop = 1` ทั้งสอง actor, `canvasGroupCompatible = 0` → pmaVertexColors = true)

ผล `Verify` ท้าย build (edit mode): `cameras=1 canvases=1 buttons=7 errors=0`;
FullBody 745.2 px = 69% (fit 67.9%), Chibi 260.7 px = 24.1% (fit 24.3%), FullBody x = −623, Chibi x = +427 (ตำแหน่ง rect หลังชดเชย mesh center)

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

### 5.2 ผลการรันจริงใน Play mode (batch, 2026-10-23)

`PlayCheckBatch` เปิด `Demo.unity` → `EditorApplication.EnterPlaymode()` → รอ 20 เฟรม → ตรวจ → ออกด้วย exit code
(รอบล่าสุด: **PASS, exit 0, errors = 0**; ใช้ `-batchmode` ไม่ใส่ `-nographics`/`-quit`)

- FULL BODY: `verts=486 shader=Spine/SkeletonGraphic skin=demo_combined_0 animation=idle1 t=0.13` → **739.6 px = 68.5%** ของ canvas
- CHIBI: `verts=126 shader=Spine/SkeletonGraphic skin=demo_combined_0 animation=stand t=0.13` → **251.5 px = 23.3%**
- ปุ่ม 4 ตัว (Next Skin / Next Animation / Toggle Gray / Flip X) เรียกผ่าน `onClick.Invoke()` ได้ ไม่มี exception, verts รวมคงที่ 612
- หลัง Toggle Gray: material ของ FullBody เป็น shader `Spine/SkeletonGraphic Grayscale`
- ภาพ 1920×1080 จาก `SaveScreenshot` (วิเคราะห์ pixel): FullBody อยู่ซีกซ้าย (x 116–843), Chibi อยู่ซีกขวา (x 1274–1619) ไม่มีตัวไหนหลุดขอบ
- เก็บภาพ: `Assets/_LocalReference/Screenshots/demo_built_idle1.png` (ก่อนกดปุ่ม) และ `demo_after_buttons.png` (หลังกด Flip/Gray)

**Observed (ทำไมไม่ใช่ 70/25 เป๊ะ)**: bbox เปลี่ยนตามเฟรม (FullBody idle1 กว้างขึ้น 1.02 เท่า สูงขึ้น 1.09 เท่า) →
fit ที่ 97% ของเป้าหมาย เพื่อให้ bbox ที่โตขึ้นยังไม่ล้นลงไปใต้แถบปุ่ม; ตัวเลข 68.5% / 23.3% คือค่าที่วัดได้จริงตอน play

### 5.3 กลไก batch play check (Observed)

- `EditorApplication.EnterPlaymode()` ใน `-executeMethod` แล้ว domain reload จะล้าง static ทั้งหมด → สถานะ "กำลังตรวจ" เก็บใน `EditorPrefs`
  และมี `[InitializeOnLoadMethod]` ลงทะเบียน `EditorApplication.update` ใหม่หลัง reload
- รอ `EditorApplication.isPlaying` แล้ว 20 เฟรม (ให้ `Start()` สร้าง actor + animation ได้ tick) → ตรวจ → `EditorApplication.Exit(0/2)`
- timeout 180 วินาที → exit 1 (กันค้างแบบรอบก่อนที่ต้อง kill process)
- ต้องมี graphics device (ไม่ใส่ `-nographics`) ไม่งั้น `SaveScreenshot` render ไม่ได้

---

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
4. **Actor ถูกสร้างใหม่ทุกครั้งที่โหลดซีน**: `Start()` จะ `Build()` ใหม่ (destroy/recreate `SpineGraphic`) ในโหมด Editor การ destroy เป็นแบบ deferred
   จึงอาจเห็น `SpineGraphic` เปล่า (verts=0) ค้างอยู่ใน hierarchy ชั่วคราว — ใน play mode ไม่มีปัญหา
5. **คำเตือนตอน AddComponent**: ตามข้อ 5 (แก้ไม่ได้ถ้าไม่แก้ `SpineUiActor` ซึ่งห้ามแก้ในรอบนี้)

---

## 7. วิธีใช้

### 7.1 เปิดดู

1. เปิด Unity Editor ที่โปรเจกต์ `my_clone`
2. เปิด `Assets/_LocalReference/Demo.unity` (**ไม่ใช่ซีน Untitled ที่ค้างอยู่** — ถ้ามีซีน Untitled ให้ปิดทิ้งโดยไม่ต้องบันทึก)
3. กด Play: `SpineDemoController.Start()` จะ `Build()` actor ใหม่ (เพราะ `SpineUiActor.m_Graphic` ไม่ถูก serialize),
   รวม skin กลุ่มละตัว แล้วเล่น `InitialAnimation` ที่เซ็ตไว้ (`idle1` / `stand`) — ป้าย Info จะแสดง bones/slots/skins/anims + skin/animation ปัจจุบัน

### 7.2 ปุ่ม

| ปุ่ม | เรียก | ผลที่เห็น |
|---|---|---|
| **Next Skin** | `SpineDemoController.NextSkin()` | เลื่อน variant ถัดไปของทุกกลุ่ม skin พร้อมกัน (FullBody: `body/face/head_1113002_1`; Chibi: `body/head_1123011_1..7`) แล้ว refresh ป้าย Info |
| **Next Animation** | `NextAnimation()` | เล่นแอนิเมชันถัดไปในรายการของ skeleton (คนละรายการระหว่างสองตัว) แบบ loop |
| **Flip X** | `ToggleFlip()` | `SpineUiActor.SetFlipX(!FlipX)` ทั้งสองตัว (กลับด้านซ้าย–ขวา) |
| **Toggle Gray** | `ToggleGray()` | สลับ material ปกติ ↔ `Spine/SkeletonGraphic Grayscale` (`_GrayAmount = 1`); กดอีกครั้งคืนสี |
| Mount | `ToggleMount()` | `fullBody.Mount(weaponData, "w_jian/w_jian_300101")` + `MountToBone("wuqi")`; กดอีกครั้ง = `UnMount()` (ซ่อน ไม่ลบ) |
| Audit Skins | `AuditSkins()` | log ทุก skin variant: slots ที่มี attachment, verts, ขนาด mesh, slot ที่ว่าง, สรุป `complete variants` |
| Screenshot | `CaptureScreenshot()` | render canvas 1920×1080 ลง `Assets/_LocalReference/Screenshots/demo_NN_<anim>_variant<k>.png` |

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
