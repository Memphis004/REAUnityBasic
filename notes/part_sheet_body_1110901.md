# Part Sheet — body_1110901

ชีตตารางคงที่ (part sheet) สำหรับใช้เป็นแม่แบบให้ Qwen-Image-Edit เจนชิ้นส่วนชุดใหม่
สร้างจาก `SkeletonDataAsset` ของ `body_1110901` ด้วย Editor tool ใหม่ + tool นำชีตที่เจนกลับเข้ามา
ไม่มีการแก้ไฟล์ใน `_LocalReference/Spine/` ไม่คัดลอกโค้ดหรือ DLL ของเกม และยังไม่ประกอบกลับเข้า
`SkeletonDataAsset` ในรอบนี้

- วันที่รัน: 2026-10-24 (build 20:27, import self-test 20:27)
- Unity: 2021.3.45f2, project `C:\Workspace\REA\my_clone`, spine 3.8.75
- Asset ต้นทาง (อ่านอย่างเดียว): `Assets/_LocalReference/Spine/body_1110901/body_1110901_SkeletonData.asset`
- ผลลัพธ์: `extracted/part_sheets/body_1110901/` (นอก `Assets`, ถูก ignore ด้วย `extracted/*` — `git check-ignore` ผ่าน)
- โน้ตนี้: `notes/part_sheet_body_1110901.md` (tracked)

## 1. Tool ที่เพิ่ม

| Tool | ไฟล์ | เมนู |
|---|---|---|
| Build Part Sheet | `Assets/Editor/PartSheetExporter.cs` | `Tools/LocalReference/Build Part Sheet` (alias `Tools/Local Reference/...`) |
| Import Generated Sheet | `Assets/Editor/PartSheetImporter.cs` | `Tools/LocalReference/Import Generated Sheet` |
| group map / geometry / pixel ops | `PartSheetGroups.cs`, `PartSheetLayout.cs`, `PartSheetImage.cs` | — |

- เรียกจากโค้ดได้: `MyClone.EditorTools.PartSheetExporter.Export(options)` / `PartSheetImporter.Import(options)`
- ใช้ซ้ำของเดิม: `ReferenceSheetExporter.RenderSession` (SkeletonAnimation + กล้อง orthographic +
  RenderTexture, ไม่มี MSAA, ไม่มี mipmap, `filterMode.Point`), `ReferenceSheetExporter.ResolveSkins`,
  `ReferenceSheetGroups` (walk slot / draw order / skin part), `SpineActorBase.DetectSkinGroups`
  — แก้ไฟล์เดิมเพียง 2 จุด: เปลี่ยน `RenderSession`/`ResolveSkins` เป็น `internal` และคืน active scene
  หลังสร้าง additive scene ( Unity ไม่ยอมสร้าง additive scene ตัวที่สองขณะ temp scene ยัง active )
- **หมายเหตุเรื่อง `part_groups.json`:** ไฟล์ชื่อนี้ไม่มีในโปรเจกต์ (ค้นทั้ง repo = 0). ผลที่มีอยู่จริงคือ
  `Parts/body_1110901/parts_index.json` (จาก Slice Atlas Parts) และ
  `extracted/ref_sheets/body_1110901/layers_groups.json` (จาก Export Reference Sheet) — tool นี้ใช้
  rect ของ atlas region จาก runtime (`AtlasRegion`) แล้ว **cross-check กับ `parts_index.json`**
  (ผล: 18/18 region ตรงกันทั้ง `xy`, `size`, `rotate`)

## 2. ไฟล์ที่ได้ (`extracted/part_sheets/body_1110901/`)

| ไฟล์ | ขนาด | ใช้ทำอะไร |
|---|---|---|
| `sheet.png` | 1536×1024 | ชีตหลัก พื้นหลัง `#FF00FF` — **input ของ Qwen** |
| `sheet_alpha.png` | 1536×1024 | ชีตเดียวกันแบบโปร่งใส (straight alpha) |
| `sheet_template.png` | 1536×1024 | silhouette ทึบโทนเดียวต่อกลุ่ม + ตัวเต็มโทนเทากลาง (ไม่มีภาพเกม) |
| `layout.json` | 16 KB | ช่อง → กลุ่ม, rect, placement (สำหรับ import) |
| `manifest.json` | 43 KB | slot/attachment ต่อกลุ่ม, transform ของ render, bounds, ผลตรวจ |
| `masks/<group>.png` | 204×341 ×11 | รูปทรง alpha ต้นฉบับของกลุ่ม (ขาว + alpha) ในช่อง ใช้ตัดกลับ |
| `native/<group>.png`, `<group>_alpha.png` | 126×161 ×11 | render 1:1 ระดับ texel (magenta / โปร่งใส) — ไม่อยู่ในสเปก, ใช้ตรวจและ map กลับ atlas |
| `native/full.png`, `full_alpha.png` | 126×161 | ตัวเต็ม 1:1 (ใช้เทียบ reassembly) |
| `_viewonly_overview.png` | 1536×1024 | ชีต + ป้ายชื่อช่อง (**ห้าม**ใช้เป็น input ของ Qwen) |
| `sheet_groups.json` | 7.6 KB | กลุ่มที่แก้ไขได้ (เขียนครั้งแรกครั้งเดียว, ค่าเริ่มต้นที่ tool เสนอ) |
| `import_selftest/` | — | ผล Import แบบ self-test (ใช้ `sheet.png` เองเป็น input) — ดู §6 |

`premultiplied` คือรูปแบบที่ renderer เขียน; ไฟล์ `*_alpha.png` แปลงเป็น straight alpha ก่อนเซฟ
(หลักฐาน: 6,566 px ที่ RGB > alpha ซึ่งไฟล์ PMA ทำไม่ได้)

## 3. วิธีทำงานของ Build (A1–A5)

### 3.1 กลุ่ม (A1)

1. ประกอบสกิน `body_1110901_1 + head_1110901_1 + face_1110901_1` (เหมือน `SpineUiActor`), `SetToSetupPose()`
2. อ่าน slot ทั้ง 23 ตาม draw order + attachment + skin part
3. เสนอชื่อกลุ่มตามชื่อ slot ที่พบจริง (name table) แล้วเขียนลง `sheet_groups.json`
   (`slots` = สมาชิกที่แก้ได้, `slotsInDrawOrder` / `drawOrderMin..Max` = ลำดับจริง)
4. slot ที่ไม่มีกฎ → อยู่ใน `fallbackGroup` = `other` และถูกรายงานใน `unassignedSlots`
   **ไม่มีการเดาเงียบ ๆ**

### 3.2 render (A2)

- `SkeletonAnimation` (ผ่าน `SpineWorldActor`) ในฉาก additive ชั่วคราว → กล้อง orthographic → `RenderTexture` ARGB32
  (`antiAliasing = 1`, `useMipMap = false`, `filterMode.Point`)
- ซ่อน slot นอกกลุ่มด้วย `slot.Attachment = null` บนอินสแตนซ์ skeleton (ไม่แตะ asset) แล้ว `SetToSetupPose()` ก่อนทุกกลุ่ม
- **native scale = 1 texel = 1 pixel:** `1 / SkeletonDataAsset.scale (0.01)` = 100 px ต่อ 1 หน่วย skeleton
  - ตรวจด้วย RegionAttachment: region `1110901_1wuguan` (32 px) → วัดได้ 100.31 px/unit → error **0.31 %**
  - canvas 1:1 = 126×161 px, origin ของ skeleton = (37, 5) จากมุมล่างซ้าย (ปัดเป็นจำนวนเต็ม
    เพื่อให้ texel grid ของ atlas ตรงกับ pixel)
- **sheet scale = 1.514563** ตัวเดียวทั้งชีต, ผูกด้วย *ความกว้างของ `sash_or_ribbon`*
  (103 px native + margin 24×2 → 156 px ในช่อง 204 px)
- แต่ละชิ้นถูกวางกึ่งกลางช่อง (ทั้งแนวตั้ง/แนวนอน) ไม่หมุน ไม่มีเส้นกรอบ ไม่มีตัวอักษร
- ตัวเต็มอยู่ขวา 1/3 (`x = 1024..1535`) กึ่งกลางช่อง ใช้ scale เดียวกัน (ได้ 162×210 px ในช่อง 512×1024)

### 3.3 เรขาคณิตของชีต (A3)

| รายการ | ค่า |
|---|---|
| canvas | 1536×1024, margin 24 px, พื้นหลัง `#FF00FF` |
| grid (ซ้าย 2/3) | 5×3, ช่อง **204×341** เท่ากันทุกช่อง, origin `(2, 0)` (top-left) |
| พื้นที่ grid จริง | 1020×1023 → เหลือแถบ 4 px (ขวา) และ 1 px (ล่าง) เป็นพื้นหลัง |
| ช่องว่างเนื้อหา | 156×293 px (204−48, 341−48) |
| แถบตัวเต็ม (ขวา 1/3) | `x=1024, y=0, 512×1024` |
| ลำดับช่อง | คงที่ตาม `layout.json` = ลำดับเดียวกับ `PartSheetGroups.GroupOrder` |

## 4. กลุ่ม → slot → ช่อง (A1/A4)

| ช่อง | กลุ่ม | slot (draw order) | draw | native px | ในช่อง px | margin | atlas region |
|---:|---|---|---|---:|---|---:|---|
| 0 | hair_front | `hair_f1, hair_f2` | 20..22 | 51×63 | 79×96 | 62 | `1110901_1bin`, `1110901_1liuhai` |
| 1 | hair_back | `hair_b` | 0 | 37×75 | 57×113 | 73 | `1110901_1houfa` |
| 2 | head | `head` | 19 | 55×76 | 83×114 | 60 | `1110901_1tou` |
| 3 | face_open | `face` | 21 | 32×22 | 49×33 | 77 | `1110901_1wuguan` |
| 4 | face_alt | — | — | — | — | — | — (ว่าง) |
| 5 | torso | `body` | 13 | 43×79 | 64×120 | 70 | `1110901_1shenti` |
| 6 | lower_body | `zuohoubai, youhoubai, youbai, zuobai` | 7..12 | 64×53 | 97×81 | 53 | `…zuohoubai`, `…youhoubai`, `…youbai`, `…zuobai` |
| 7 | arm_L | `zuoshou, zuojian` | 16..17 | 23×32 | 34×49 | 85 | `…zuoshou`, `…jian` |
| 8 | arm_R | `youshou, youjian` | 1..14 | 25×32 | 37×49 | 83 | `…youshou`, `…youjian` |
| 9 | leg_L | `zuojiao` | 11 | 24×50 | 36×76 | 83 | `…zuojiao` |
| 10 | leg_R | `youjiao` | 9 | 18×51 | 27×77 | 88 | `…youjiao` |
| 11 | sash_or_ribbon | `piaodai2, piaodai1` | 6..15 | 103×62 | 156×94 | 24 | `…piaodai2`, `…piaodai1` |
| 12 | accessory | `w_dao, w_zhang, w_qiang, w_jian` | 2..5 | — | — | — | — (ว่าง) |
| 13 | other | `root_boundingBox` | 18 | — | — | — | — (ว่าง) |
| 14 | (ว่าง) | — | — | — | — | — | — |

- 15 ช่องแต่มี 14 กลุ่ม → ช่อง 14 ว่าง (ไม่มีชื่อกลุ่มใน `layout.json`)
- `margin` = ระยะจากขอบช่องที่แคบที่สุดของชิ้น (ต้อง ≥ 24; ค่าต่ำสุดจริง = 24 ที่ `sash_or_ribbon`)

### 4.1 ชิ้นที่จัดไม่ได้ / กลุ่มว่าง (รายงานตรง ๆ)

| สิ่งที่พบ | สถานะ |
|---|---|
| `root_boundingBox` | ไม่มีกฎจับ → `other` (รายงานใน `sheet_groups.json.unassignedSlots`); attachment เป็น BoundingBox จึงไม่วาดพิกเซล |
| `w_dao, w_zhang, w_qiang, w_jian` | อยู่ใน `accessory` แต่สกินที่ประกอบไม่มี attachment ให้เลย → กลุ่มว่าง |
| `face_alt` | skeleton มี face skin เดียว (`face_1110901_1`) → ไม่มี expression อื่นให้แยก |
| `arm_L/arm_R`, `leg_L/leg_R` | skeleton นี้ไม่มี mesh แขน/ขาแยก: `youjian/zuojian` = ไหล่, `youshou/zuoshou` = มือ, `youjiao/zuojiao` = เท้า จึงจัด ไหล่+มือ เป็น arm, เท้า เป็น leg — เป็นการตีความ (inferred) ไม่ใช่ชื่อที่ asset ประกาศ |
| `hair_front` + หัวเปล่า | ไม่ต้องรวมหัวเปล่าเข้า `hair_front` เพราะมีกลุ่ม `head` แยกอยู่แล้ว — แต่ **ภาพของ slot `head` มีผม/จ่อมอยู่ในตัว** (ดู §8) |

## 5. ผลตรวจเชิงตัวเลขของ Build (A6)

อ่านจากไฟล์ PNG ที่เซฟแล้ว (ไม่ใช่ค่าที่คำนวณค้างในหน่วยความจำ) — `manifest.json.checks`

| การตรวจ | ค่า | เกณฑ์ | ผล |
|---|---|---|---|
| ช่องถูกตัดขอบ | 11 ช่องที่มีเนื้อหา, **0 ช่องถูกตัด**, margin ต่ำสุด **24 px** | margin ≥ 24 | ผ่าน |
| พื้นหลังนอกตัว = `#FF00FF` แท้ | **1,525,840 / 1,525,840 px** ตรงเป๊ะ, 0 px อื่น, **0 px ใกล้แต่ไม่เท่า** | ต้อง 0 ทั้งคู่ | ผ่าน |
| reassembly: รวมกลุ่มตาม draw order เทียบตัวเต็ม (1:1, alpha) | **mismatch 0 px / 20,286 px = 0.0000 %** (tolerance 2/255), mean alpha diff 0.015 | ≤ 2 % | ผ่าน |
| ขอบดำจาก premultiplied | **near-black fringe 2 px** (alpha ≥ 16 และ max RGB ≤ 12) เทียบ baseline ของ native 1:1 = **0 px**; key spill 0 px | ใกล้ 0 | ผ่าน (2 px มาจากการ resample ขอบเข้ม ไม่ใช่ PMA ค้าง) |
| หลักฐานว่า `_alpha` เป็น straight alpha | **6,566 px** ที่ RGB > alpha (PMA ทำไม่ได้) | > 0 | ผ่าน |
| edge heuristic (เข้มกว่าเพื่อนบ้านทึบ > 60 %) | 426 / 1,243 px (34 %) | — | **ไม่ใช่ error**: เป็นเส้นตัดดำของอาร์ต (native 1:1 ก็เป็นแบบเดียวกัน); ตัวชี้ขาดคือ near-black fringe ข้างบน |
| `1 texel = 1 pixel` | 100 px/unit จาก `asset.scale`; วัดจริง 100.31 px/unit | error ≤ 2 % | ผ่าน (0.31 %) |
| 18 atlas region ตรงกับ `parts_index.json` | 18/18 (`xy`, `size`, `rotate`) | ต้องตรงทั้งหมด | ผ่าน |
| mask ตรงกับ `contentRect` ใน `layout.json` | 11/11 ตรง (ต่าง ≤ 1 px) | — | ผ่าน |

หมายเหตุลำดับชั้น (`checks.groupOrderNotes`): กลุ่มที่มี slot คั่นกลางกลุ่มอื่น
(`arm_R` 1..14, `sash_or_ribbon` 6..15, `lower_body` 7..12, `hair_front` 20..22 ที่คั่น `face_open`)
— ไม่กระทบ reassembly เพราะตรวจด้วย alpha (ที่รวมกันไม่ขึ้นกับลำดับ) แต่ถ้าต้องการตรงเป๊ะราย slot
ต้องแยกกลุ่มให้ตรงลำดับ

## 6. Import Generated Sheet (B)

### 6.1 pipeline

1. โหลด `layout.json` + `manifest.json` + `generated_sheet.png`; **ขนาดต้องเท่า `sheet.png`** เป๊ะ ไม่งั้นหยุด
2. ตัดตาม `layout.cells[].rect` → ลบสี key → straight alpha แล้วเซฟ `import/groups/<group>.png`
3. **chroma key `#FF00FF`** (สูตรใหม่ที่ใช้จริง):
   - color range: ระยะ RGB ถึง `#FF00FF` ≤ `keyTolerance` (24) → alpha 0
   - alpha จาก keyness: `alpha = 1 - clamp01((min(R,B) - G) / 255)` (ตรงกับ blend จริงทั้งช่วง
     เพราะ key มี G = 0; ใช้ได้กับขาว/ดำ/ผิว/ผ้า)
   - decontaminate: `F = (C - (1-alpha)·K) / alpha`
   - defringe: alpha < `minAlpha` (8) หรือสีที่ unmix แล้วยังใกล้ key → ทิ้ง
4. เทียบกับ `masks/<group>.png` (alpha, threshold 8):
   - `spill_fraction` = (alpha ใหม่ > 8 นอก mask) / (alpha ใหม่ > 8 ทั้งหมด)
   - `coverage_fraction` = (ใน mask แต่ภาพใหม่ alpha ≤ 8) / (พื้นที่ mask)
   - `solid_coverage_fraction` = เช่นเดียวกันแต่คิดเฉพาะแกนทึบของ mask (alpha ≥ 64)
5. เขียนกลับ atlas: ต่อ attachment สร้าง affine least-squares จาก **page UV → world vertex**
   (`RegionAttachment` 4 มุม / `MeshAttachment` ทุก vertex ที่ `UpdateUVs()` แล้ว) → ผกผันกลับด้วย
   `layout.cell.placement` → sample ภาพช่อง → เขียนลง **สำเนา** หน้า atlas
   (region ที่ rotate อยู่ก็ทำงานได้เพราะ map ทีละ page texel ผ่าน UV)
6. เขียน `body_1110901_new.png` (PMA เหมือนต้นฉบับ) + `import_report.json`

### 6.2 Self-test (ใช้ `sheet.png` เองเป็น `generated_sheet.png`)

รอบนี้ยังไม่มีภาพจาก Qwen จึงรันด้วย `sheet.png` → ผลที่ควรได้คือ spill ≈ 0 และ coverage เหลือเฉพาะขอบจาง

| กลุ่ม | spill | coverage | solid coverage | region ที่เขียน |
|---|---:|---:|---:|---:|
| hair_front | 0.00 % | 9.40 % | 0.00 % | 2 |
| hair_back | 0.00 % | 4.37 % | 0.00 % | 1 |
| head | 0.00 % | 1.66 % | 0.00 % | 1 |
| face_open | 0.00 % | 3.33 % | 0.00 % | 1 |
| torso | 0.00 % | 2.93 % | 0.00 % | 1 |
| lower_body | 0.00 % | 2.33 % | 0.00 % | 4 |
| arm_L | 0.00 % | 3.98 % | 0.00 % | 2 |
| arm_R | 0.00 % | 4.17 % | 0.00 % | 2 |
| leg_L | 0.00 % | 2.35 % | 0.00 % | 1 |
| leg_R | 0.00 % | 2.58 % | 0.00 % | 1 |
| sash_or_ribbon | 0.00 % | 7.23 % | 0.00 % | 2 |

- ไม่มีกลุ่มใดเกิน 0.15 (`overLimitGroups = []`)
- coverage ที่เหลือคือพิกเซลขอบที่ alpha ต่ำมาก (ผสม magenta แล้วระยะสีใกล้ key มาก) — ข้อมูลสี
  ไม่พอจะกู้คืนจากชีตที่ composite บน magenta; **solid core 0.00 % ทุกกลุ่ม**
- 18/18 region ถูกเขียน; map residual 0.000–0.370 px

### 6.3 ตรวจว่าการวัดตอบสนองจริง (shifted test)

สร้างภาพทดสอบ `artifacts/partsheet_shifted_test.png` = `sheet.png` แต่เลื่อนเนื้อหาช่อง `head` ไป +7,+5 px
แล้ว import (โฟลเดอร์ `import_shifted_test` ถูกลบทิ้งหลังวัด):

| กลุ่ม | spill | coverage |
|---|---:|---:|
| head | **18.75 %** | **19.62 %** (solid 18.09 %) → OVER LIMIT |
| กลุ่มอื่น (ไม่ถูกแก้) | 0.00 % | เท่าเดิม |

→ metric จับการเลื่อน/รูปร่างเปลี่ยนได้จริง และรายงานเฉพาะกลุ่มที่เกิน 0.15

### 6.4 เขียนกลับหน้า atlas (B3)

เทียบ `body_1110901_new.png` กับ `body_1110901.png` ต้นฉบับ (self-test คือภาพเดียวกัน):

| การวัด | ค่า |
|---|---|
| RGB mean abs diff | 3.81 / 255 |
| px ที่ diff > 32 | 2,471 / 92,160 (2.7 %) — เกือบทั้งหมดอยู่ที่ขอบ (อาร์ตถูก resample 1.51× แล้วย่อกลับ) |
| alpha mean abs diff | 4.54 / 255 |
| px ที่ |Δalpha| > 32 | 1,970 / 92,160 (2.1 %) |
| region ของสกิน `_2`, `_3` (ไม่ได้ประกอบ) | **diff = 0 ทุกพิกเซล** (ไม่ถูกแตะ) |

- หน้า atlas ใหม่เป็น **premultiplied** เหมือนต้นฉบับ (ไม่ได้ import เข้า Unity ไม่มี `.meta`)
- ขนาดหน้า atlas ยังเป็น 1152×80 และ **rect ของทุก region ไม่เปลี่ยน** → UV ใน `.skel` ยังใช้ได้
- ข้อจำกัด: กลุ่มที่มีหลาย region (`hair_front` 2, `lower_body` 4, `arm_L` 2, `arm_R` 2,
  `sash_or_ribbon` 2) เขียนแต่ละ region จาก *ภาพรวมของกลุ่ม* ดังนั้นตรงบริเวณที่ชิ้นในกลุ่มทับกัน
  alpha ที่เขียนลง region จะเป็น alpha รวมของทั้งกลุ่ม (report เตือนทุกกลุ่มเหล่านี้)

### 6.5 ต้องใช้อะไรเพิ่มถ้าจะประกอบกลับเป็น SkeletonData (B4)

`import_report.json.dataNeededForSkeleton` (สรุป):

1. `SpineAtlasAsset` ใหม่ (material ใช้ `body_1110901_new.png`, shader/material เดิม, page size เดิม)
2. `.atlas.txt` เดิม (rect ไม่เปลี่ยน) + `.skel.bytes` เดิม → **ไม่ต้องสร้าง SkeletonDataAsset ใหม่**
   แค่ชี้ atlas asset ไปที่ texture ใหม่
3. ถ้ารูปร่างใหม่ยื่นออกนอก `masks/<group>.png` → ต้อง re-author mesh vertices/weights ใน Spine
   (tool ไม่สร้าง geometry/bone ให้) และถ้ามีชิ้นใหม่ต้องเพิ่ม slot/attachment/skin
4. ถ้าต้อง repack region (ชิ้นใหญ่ขึ้น/ย้าย) ต้อง export .skel ใหม่ให้ UV ตรงกับ atlas ใหม่
5. texture ใหม่ต้อง import ด้วยค่าเดียวกับหน้าเดิม (PMA: `Alpha Is Transparency` ปิด, no mipmap, no compression)

## 7. วิธีใช้ / วิธีแก้

- เปลี่ยนว่ากลุ่มไหนได้ slot ใด: แก้ `sheet_groups.json` (`groups[].slots` ชนะ prefix) แล้วกด Build ซ้ำ
  (layout/ช่อง/ไฟล์อื่นถูกเขียนใหม่ทุกครั้ง)
- ถ้าอยากได้ชิ้นใหญ่ขึ้นในช่อง: ลด `margin` หรือเปลี่ยน `columns/rows` ในหน้าต่าง Build
  — แต่ขนาดช่องและ 5×3 ถูกตรึงตามสเปกในรอบนี้
- `sheet.png`/`sheet_alpha.png`/`sheet_template.png` เป็น input ได้; `_viewonly_overview.png` **ห้าม**
- Import: เลือกโฟลเดอร์ `<id>` + ไฟล์ที่เจน (ต้อง 1536×1024) → ได้ `import/groups/*.png`,
  `import/import_report.json`, `import/body_1110901_new.png`

## 8. observed / inferred / unknown

### observed (อ่าน/วัดจากไฟล์จริง)

- 23 slot, 8 สกิน, ประกอบ 3 สกิน: `body_1110901_1 + head_1110901_1 + face_1110901_1` → 18 attachment
  (17 Mesh + 1 Region) ตรงกับ `manifest.groups[].atlasRegions` (18 region, ตรง `parts_index.json` ทั้งหมด)
- ชีต 1536×1024, 11/15 ช่องมีเนื้อหา, ช่อง 14 ว่าง, 3 กลุ่มว่าง (`face_alt`, `accessory`, `other`)
- พื้นหลัง magenta เป๊ะ 1,525,840 px (ทุกพิกเซลที่ alpha = 0), ไม่มีพิกเซล "ใกล้แต่ไม่เท่า"
- reassembly ของ 11 กลุ่ม (1:1, ตาม draw order) เทียบตัวเต็ม = 0 mismatch / 20,286 px, mean alpha diff 0.0150
- margin ต่ำสุด 24 px (ที่ `sash_or_ribbon`) — ไม่มีช่องใดถูกตัด
- `1 texel = 1 pixel` = 100 px/unit; วัดจาก region `wuguan` ได้ 100.31 px/unit (0.31 %)
- sheet scale = 1.514563, ผูกด้วยความกว้างของ `sash_or_ribbon` (103 px native → 156 px ในช่อง)
- ภาพ slot `head` (`1110901_1tou`) **มีผม/จ่อมอยู่ในตัว** ไม่ใช่หัวเปล่า; `hair_front`
  (`bin`+`liuhai`) เป็นปอยผมเส้นเล็ก; `hair_back` (`houfa`) เป็นผมด้านหลัง
- `accessory` มี 4 slot แต่ 0 attachment ในสกินที่ประกอบ; `root_boundingBox` เป็น BoundingBox (ไม่วาด)
- import self-test: spill 0.00 % ทุกกลุ่ม, coverage 1.66–9.40 % (solid 0.00 %), 18/18 region เขียนสำเร็จ
- shifted test (เลื่อนช่อง head 7,5 px): head spill 18.75 % / coverage 19.62 % → OVER LIMIT, กลุ่มอื่น 0
- หน้า atlas ใหม่: RGB mean diff 3.81, 2.7 % px > 32, alpha mean 4.54; region ของสกิน `_2/_3` diff 0

### inferred (ตีความจากข้อมูล ยังไม่ได้ยืนยันด้วยแหล่งอื่น)

- การจัด `arm_L/arm_R` = ไหล่+มือ และ `leg_L/leg_R` = เท้า เป็นการตีความจากชื่อ slot
  (`jian` ไหล่, `shou` มือ, `jiao` เท้า) เพราะ skeleton นี้ไม่มี mesh แขน/ขาดวงเดียว
- coverage ที่เหลือใน self-test มาจากพิกเซลขอบที่ alpha ต่ำ (< ~10 %) ซึ่งข้อมูลสีบน magenta
  ไม่พอจะกู้ alpha กลับ — น่าจะเป็นสาเหตุเดียว (solid core เป็น 0 ทั้งหมด)
- `2 px near-black fringe` เกิดจากการ resample ขอบเข้มขึ้นในชีต (native 1:1 = 0 px) ไม่ใช่ PMA ค้าง
- 426/1,243 "edge เข้มกว่าเพื่อนบ้าน" คือเส้นตัดดำของอาร์ต ไม่ใช่ artefact (native 1:1 ก็มีสัดส่วนใกล้กัน)
- ชิ้นที่เห็นเล็กในช่อง (เช่น arm 34×49 px) เป็นผลจากกติกา "สเกลเดียวทั้งชีต ผูกด้วยกลุ่มที่ใหญ่สุด"
  (sash กว้าง 103 px) ไม่ใช่ bug
- หน้า atlas ที่เขียนใหม่ใช้แทนหน้าเดิมได้เลยถ้า rect ไม่เปลี่ยน — แต่ยังไม่ได้ลอง import เข้า Unity
  และยังไม่ได้ render SkeletonData ที่ชี้หน้าใหม่

### unknown (ยังไม่รู้ / ยังไม่ได้ทดสอบ)

- Qwen-Image-Edit จะให้ชิ้นที่ใช้ได้จริงแค่ไหนกับชีตนี้ (ยังไม่มีภาพจาก Qwen — self-test ใช้ sheet.png เอง)
- คุณภาพเมื่อภาพที่เจนเป็นอาร์ตความละเอียดสูง/มีเงา: chroma key ที่ alpha จาก keyness จะได้ขอบเนียนแค่ไหน
  และ spill/coverage จะต่ำพอหรือไม่ (ยังไม่ได้ทดสอบกับภาพจริง)
- เนื้อหาสีม่วง/แดงอมม่วงจะกู้ไม่ได้ด้วย key `#FF00FF` (ข้อจำกัดของวิธี key เอง) — ยังไม่ได้วัดผลกระทบกับชุดจริง
- กลุ่มที่มีหลาย region และชิ้นทับกัน: alpha ที่เขียนลงแต่ละ region เป็น composite ของกลุ่ม
  (เตือนใน report) — ยังไม่ได้แยกด้วย per-attachment mask
- การประกอบกลับเป็น SkeletonDataAsset/สกินใหม่ (รวม mesh ใหม่สำหรับชิ้นที่รูปร่างเปลี่ยน) ยังไม่ทำ
- ยังไม่ได้ทดสอบกับ id อื่น (`1110011`, `410041_1`, `460011_1`, `570036`) — name table ของกลุ่ม
  จะจัดได้เฉพาะชื่อ slot ที่รู้จัก; ที่เหลือจะตกไป `other` และถูกรายงาน
- `include` ป้ายชื่อใน `_viewonly_overview.png` เป็น bitmap 5×7 ในตัว tool (ไม่มีฟอนต์ asset) — ยังไม่ได้ตรวจ
  การอ่านด้วยตาในทุกความละเอียด
