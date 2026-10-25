# Reference sheets — body_1110901

ชุดภาพอ้างอิงสำหรับใช้เป็นแม่แบบ (reference) ใน Qwen-Image-Edit สร้างจาก `SkeletonDataAsset`
ของ `body_1110901` ด้วย Editor tool ตัวใหม่ ไม่มีการแก้ `SpineUiActor` ไม่คัดลอก DLL
และไม่มีการเขียนระบบประกอบ mesh เอง

- วันที่รัน: 2026-10-24
- Tool: `Tools/Local Reference/Export Reference Sheet` (alias: `Tools/LocalReference/Export Reference Sheet`)
  → `my_clone/Assets/Editor/ReferenceSheetExporter.cs`, `ReferenceSheetGroups.cs`, `ReferenceSheetImage.cs`
- Asset ต้นทาง: `Assets/_LocalReference/Spine/body_1110901/body_1110901_SkeletonData.asset` (spine 3.8.75)
- ผลลัพธ์: `extracted/ref_sheets/body_1110901/` (นอก `Assets`, ถูก ignore โดย `extracted/*`)
- ยัง**ไม่**ทำการประกอบกลับเข้า Spine (`SkeletonDataAsset`/สกินใหม่) ในรอบนี้

## 1. วิธีทำงานของ tool

1. สร้าง `SkeletonAnimation` ชั่วคราวผ่าน `SpineWorldActor` (คลาสในโปรเจกต์ที่ห่อ `SkeletonRenderer` อยู่แล้ว)
   ในซีนชั่วคราวแบบ additive (ปิดซีนทิ้งเมื่อจบ) ตัวละครจึงไม่ไปแตะซีนที่เปิดอยู่
2. ประกอบสกินด้วย `SpineActorBase.SetSkins` ตามกลุ่มที่ `SpineActorBase.DetectSkinGroups` ตรวจได้
   (`body` / `head` / `face`)
3. ใส่ท่า: setup pose หรือ animation + เวลา (`state.SetAnimation` แล้ว `state.Update(time)`)
   — ทุกครั้งจะ `SetToSetupPose()` ก่อน เพื่อให้ slot ที่ซ่อนไปในเลเยอร์ก่อนหน้ากลับมา
4. คำนวณ bounds ของทั้งตัวละครในท่านั้นด้วย `Skeleton.GetBounds` แล้ว fit เข้าแคนวาสด้วย **scale เดียว**
   (fit แบบ min ของ width/height, จัดกลาง, padding)
5. กล้อง orthographic → `RenderTexture` (ARGB32, ไม่มี MSAA) → `ReadPixels`
   ภาพ magenta ประกอบจากค่า premultiplied + สีพื้นหลังในหน่วยความจำ จึงได้ `#FF00FF` เป๊ะนอกตัว
   ส่วน `_alpha.png` แปลงกลับเป็น straight alpha
6. ซ่อน slot ของกลุ่มอื่นด้วยการตั้ง `slot.Attachment = null` บน `Skeleton` อินสแตนซ์ของ tool
   (ไม่แตะ asset) แล้ว render ทีละกลุ่มด้วย fit/มุมกล้องเดิม

## 2. ไฟล์ที่ได้

| ไฟล์ | ขนาด | ใช้ทำอะไร |
|---|---|---|
| `full.png` | 1024×1536 | ภาพตัวเต็ม พื้นหลัง magenta `#FF00FF` |
| `full_alpha.png` | 1024×1536 | ภาพตัวเต็มโปร่งใส (straight alpha) |
| `<group>.png` | 1024×1536 | เฉพาะเลเยอร์ของกลุ่มนั้น พื้น magenta |
| `<group>_alpha.png` | 1024×1536 | เฉพาะเลเยอร์ของกลุ่มนั้น โปร่งใส |
| `template/<group>.png` | 1024×1536 | silhouette สีทึบโทนเดียวของกลุ่ม (พื้น magenta) |
| `template/all.png` | 1024×1536 | silhouette ทุกกลุ่มซ้อนกันตาม draw order |
| `layers_groups.json` | — | กลุ่มเลเยอร์ที่แก้ไขได้ + slot ทั้งหมดตาม draw order |
| `manifest.json` | — | ค่าที่ใช้ render, bounds ต่อกลุ่ม, slot/attachment ต่อกลุ่ม, ผลตรวจ |
| `_viewonly_contact_sheet.png` | 1632×3272 | ชีตรวมไว้ดูเอง (**ห้าม**ใช้เป็น input ของ Qwen) |

กลุ่มที่มีไฟล์: `hair_back`, `body`, `head`, `hair_front`, `face`
กลุ่มว่างเปล่า (ไม่สร้างไฟล์): `accessory`, `other`

## 3. ค่าที่ใช้ในรอบนี้

- แคนวาส 1024×1536, padding 16 px, พื้นหลัง `#FF00FF`
- ท่า: setup pose (`pose.mode = "setup"`)
- สกินที่ประกอบ: `body_1110901_1` + `face_1110901_1` + `head_1110901_1`
- scale = **844.555 px ต่อ 1 หน่วย skeleton** (fit จากความกว้าง เพราะตัวละครกว้างกว่าเมื่อเทียบแคนวาส)
- skeleton origin บนแคนวาส (พิกัด pixel จากล่าง): x = 298.647, y = 134.237
- skeleton bounds (หน่วย skeleton): x = −0.3347, y = −0.0111, w = 1.1746, h = 1.5231
- พื้นที่ที่ตัวละครกินบนแคนวาส (top-left): x = 16, y = 125, w = 992, h = 1286

## 4. เลเยอร์ / กลุ่ม

draw order คือลำดับที่ Spine วาด (0 = ล่างสุด) | painted px = จำนวนพิกเซลที่มี alpha > 0 ในไฟล์ของกลุ่ม

| group | slots | attachments | draw | painted px | bounds บนแคนวาส (x, y, w, h) |
|---|---:|---:|---|---:|---|
| hair_back | 1 | 1 | 0..0 | 89,618 | 320, 449, 320×632 |
| body | 13 | 13 | 1..17 | 311,861 | 56, 696, 952×715 |
| head | 1 | 1 | 19..19 | 176,321 | 116, 125, 464×641 |
| hair_front | 2 | 2 | 20..22 | 48,957 | 16, 363, 437×535 |
| face | 1 | 1 | 21..21 | 44,653 | 138, 538, 270×186 |
| accessory (ว่าง) | 4 | 0 | 2..5 | — | — |
| other (ว่าง) | 1 | 0 | 18..18 | — | — |

- `body` = slot `body, youshou, youjian, zuoshou, zuojian, piaodai1, piaodai2, youbai, zuobai,
  youhoubai, zuohoubai, youjiao, zuojiao` (สกินพาร์ท `body`)
- `hair_back` = `hair_b`; `hair_front` = `hair_f1`, `hair_f2`; `head` = `head`; `face` = `face`
- `accessory` = `w_dao, w_zhang, w_qiang, w_jian` — ไม่มีสกินใดให้ attachment (ว่างจริง)
- `other` = `root_boundingBox` — attachment เป็น bounding box จึงไม่วาดพิกเซล
- slot ทั้ง 23 ตัวตาม draw order อยู่ใน `layers_groups.json` (`allSlotsInDrawOrder`) และ `manifest.json` (`slots[]`)

## 5. ผลตรวจเชิงตัวเลข (อ่านจากไฟล์ PNG ที่เซฟแล้ว)

| การตรวจ | ค่า | เกณฑ์ |
|---|---|---|
| รวม alpha ของทุกกลุ่ม (ตาม draw order) เทียบ `full_alpha.png` | **mismatch 0 px / 1,572,864 = 0.0000 %** (ต่างได้ ≤ 2/255) | ต้อง ≤ 2 % → ผ่าน |
| mean alpha diff | 0.0053 (จาก 255) | — |
| กลุ่มที่อาจวางลำดับผิด | ไม่มี (`groupsLikelyMisordered = []`) | แจ้งเมื่อ mismatch > 2 % |
| near-magenta บนพื้นหลัง (alpha = 0 แต่สีไม่ใช่ `#FF00FF` เป๊ะ) | **0 px** | ต้องเป็น 0 |
| ขอบสีม่วง (พิกเซลขอบที่ R, B สูงกว่า G เกิน 40) | 159,140 / 247,915 ขอบ = 64.19 % | ดูคำอธิบายด้านล่าง |
| premultiplied ดำที่ขอบ (เทียบเพื่อนบ้านทึบที่ใกล้สุด) | 79 / 61,493 พิกเซลที่เทียบได้ = 0.13 % | ใกล้ 0 |
| หลักฐานว่าเป็น straight alpha (RGB สูงกว่า alpha) | 48,268 px (ไฟล์ premultiplied เป็นไปไม่ได้) | > 0 |

หมายเหตุที่ tool เขียนไว้ใน `manifest.json` (`checks.layerOrderNotes`):
`hair_front spans draw order 20..22 but overlaps face` — เพราะ `hair_f1` (20) อยู่หลัง `face` (21)
แต่ `hair_f2` (22) อยู่หน้า `face` กลุ่มเดียวจึงแทนลำดับราย slot ไม่ได้ทั้งหมด

> ตัวเลขชุดนี้เป็นของ **รอบที่ 2** (re-export 2026-10-24 19:43) หลังแก้ import setting ของหน้า atlas — ดู §5.1

### 5.1 รอบที่ 2 — re-export หลังแก้ import setting ของหน้า atlas

รอบแรก export ก่อนแก้ `body_1110901.png` ซึ่งตั้ง `Alpha Is Transparency` ไว้บนหน้า atlas ที่เป็น PMA
ทำให้ Unity เติม RGB ลง texel ที่ alpha = 0 และ material PMA บวกสีนั้นออกมาเป็นกรอบขาวบนใบหน้า
รอบนี้ export ซ้ำด้วยค่าเดิมทุกอย่าง (canvas 1024×1536, padding 16, magenta, setup pose,
สกิน `body_1110901_1 + face_1110901_1 + head_1110901_1`) หลังปิด `Alpha Is Transparency`

| การตรวจ (เทียบไฟล์ก่อน/หลัง) | ค่า |
|---|---|
| alpha ทุกไฟล์ (`full` + 5 กลุ่ม) | **ต่าง 0 px** — fit และซิลูเอตเดิมเป๊ะ |
| พิกเซลขาวจัด (min RGB ≥ 230) ที่หายไป | **13,757 px** ใน `full`, เพิ่มขึ้นใหม่ **0 px** |
| แยกกลุ่ม | face 770, body 9,076, head 2,095, hair_front 1,809, hair_back 2,304 |
| กรอบขาวบนใบหน้า | หายทั้งกรอบ — พิกเซลที่หายอยู่ใน bbox ของ `face` พอดี (138..407 × 538..723) |
| checks อื่นของ tool | alpha mismatch 0, near-magenta 0 (เท่าเดิม) |

- ภาพเทียบซูม 3×: `artifacts/refsheet_face_before_after.png` (ซ้าย = ก่อน, ขวา = หลัง)
- ชุดเดิมเก็บไว้ที่ `artifacts/_refsheets_before/` ทั้งโฟลเดอร์
- `layers_groups.json` ไม่ถูกเขียนทับ (เหมือนเดิมทุกไบต์); `manifest.json` อัปเดต `generated`/`checks` ของรอบนี้
- `straightEvidencePixels` ลด 63,172 → 48,268 เพราะพิกเซล RGB > alpha บางส่วนคือ RGB ที่ importer เติมมาให้
- ขอบสีม่วง/"premultiplied ดำ" ใกล้ค่าเดิม (64.19 %, 79/61,493) — เป็นการวัดบนไฟล์ magenta และแนวขอบ ไม่เกี่ยวกับ alpha bleed

## 6. วิธีใช้กับ Qwen-Image-Edit

- ใช้ `full.png` / `full_alpha.png` เป็นภาพหลัก และ `<group>.png` / `<group>_alpha.png`
  ของกลุ่มที่ต้องแก้ (เช่นแก้ผม → `hair_front` + `hair_back`) โดยทุกไฟล์ใช้ fit เดียวกัน
  ตำแหน่งเลเยอร์จึงตรงกันแบบพิกเซล
- `template/*.png` ใช้เป็นกรอบโครงสร้าง (สีทึบ ไม่มีรายละเอียดภาพ) เมื่ออยากคุมสัดส่วน/ตำแหน่งเท่านั้น
- **ห้าม**ใช้ `_viewonly_contact_sheet.png` เป็น input (เป็นภาพรวมไว้ตรวจด้วยตา และถูกตั้งชื่อนำด้วย `_` เพื่อกันสับสน)
- ถ้าต้องการเปลี่ยนว่ากลุ่มไหนได้ slot ใด: แก้ `layers_groups.json` (`groups[].slots` ชนะ `matchPrefixes`
  เสมอ) แล้ว export ซ้ำ — ไฟล์นี้ tool จะเขียนเฉพาะครั้งแรกที่ยังไม่มี จากนั้นจะไม่เขียนทับ
- ถ้าต้องการรีเซ็ตกลุ่ม: ลบ `layers_groups.json` แล้ว export ใหม่ (ได้ค่าเริ่มต้น 7 กลุ่ม)
- ค่าใน `manifest.json`: `fit.scale` + `fit.skeletonOriginXPixels/YPixels` (พิกัดจากล่าง) ใช้ประกอบกลับ
  ได้ในอนาคต; `groups[].boundsPixels` เป็นพิกัดจาก**มุมซ้ายบน** (`boundsPixelsOrigin = "top-left"`)

## 7. observed / inferred / unknown

### observed (เห็นจากผลรัน/ไฟล์จริง)

- Skeleton มี 23 slot, 8 สกิน (named 7), 3 กลุ่มสกินที่ประกอบกันได้ (`body` 3 แบบ, `head` 3 แบบ, `face` 1 แบบ)
- ผลรวม alpha ของ 5 กลุ่มตรงกับ `full_alpha.png` ทุกพิกเซล (0 mismatch) แปลว่า fit/scale/offset และ
  การซ่อน slot ทำงานถูกต้อง และไม่มี pixel ตกหล่นหรือซ้ำระหว่างกลุ่ม
- พื้นหลัง magenta เป๊ะทุกพิกเซลนอกตัว (near-magenta = 0) และ alpha PNG ไม่มีแถบดำแบบ premultiplied
  (มี 63,172 พิกเซลที่ RGB สูงกว่า alpha ซึ่งไฟล์ premultiplied ทำไม่ได้)
- `hair_b` อยู่ล่างสุดของ draw order (0) และ `hair_f2` อยู่บนสุด (22) — ตรงกับภาพที่เห็น
- `accessory` (w_dao, w_zhang, w_qiang, w_jian) ไม่มี attachment ในสกินที่ประกอบ → ไม่มีไฟล์
  และ `root_boundingBox` เป็น bounding box (ไม่วาด) → กลุ่ม `other` ว่าง
- fit ถูกจำกัดด้วยความกว้าง (992 px จาก 1024) ทำให้เหลือที่ว่างบน/ล่างมากกว่าซ้าย/ขวา

### inferred (ตีความจากข้อมูล แต่ยังไม่ได้ยืนยันด้วยแหล่งอื่น)

- การจัดกลุ่มเริ่มต้นใช้ "พาร์ทของสกิน" (`body/head/face` จาก `SpineActorBase.DetectSkinGroups`)
  แล้วจึงแยก `head` ออกเป็น `hair_back` / `hair_front` / `head` ตาม prefix — เหมาะกับ skeleton นี้
  แต่ยังไม่ได้ตรวจว่า id อื่นใช้ชื่อ slot แบบเดียวกัน
- ขอบสีม่วง 64 % ของพิกเซลขอบไม่ใช่ข้อผิดพลาดของการ render: เกิดจากการผสมพื้นหลัง magenta กับ
  พิกเซลกึ่งโปร่งใส (ค่า R และ B สูงกว่า G ตามธรรมชาติของ magenta) — ไฟล์ `_alpha.png` จึงไม่มีปัญหานี้
  และควรใช้ `_alpha.png` เมื่อนำไปแก้ภาพต่อ
- 68 พิกเซลที่ตรวจว่า "เข้มกว่าเพื่อนบ้านเกิน 60 %" น่าจะเป็นขอบเส้นดำที่ตัดกับพื้นที่สว่าง
  (heuristic ชนิดนี้มี false positive ได้) ไม่ใช่ premultiplied ค้าง — ยืนยันด้วยตัวเลข straight alpha ข้างบน
- ภาพต้นฉบับมีขนาดจริงราว 117×152 px (bounds 1.1746×1.5231 หน่วย × scale 0.01 ของ asset)
  แล้วถูกขยายขึ้นแคนวาส ~8.5 เท่า (1286/152) จึงเป็นภาพขยายของอาร์ตความละเอียดต่ำ ไม่ใช่รายละเอียดใหม่
  (atlas 1152×80 มี region สูงสุด 75 px — สอดคล้องกับตัวละครสูง ~152 px ที่ประกอบจากชิ้นส่วน)
- การประกอบสีกลับจากไฟล์กลุ่ม (เอา `<group>_alpha.png` ตาม draw order มาวางบน magenta) ต่างจาก `full.png`
  เฉพาะจุด: 0.82 % ของพิกเซลต่างเกิน 8/255 และ 0.31 % ต่างเกิน 32/255 โดยกระจุกที่บริเวณผมหน้า/ใบหน้า
  และรอยต่อกึ่งโปร่งใส — สอดคล้องกับ note เรื่อง `hair_front` คั่นกลาง `face` (การตรวจ alpha ยังได้ 0 mismatch
  เพราะ alpha รวมไม่ขึ้นกับลำดับ)
- กลุ่ม `hair_front` มี 2 slot ที่คั่นกลาง `face` (20, 21, 22) จึงไม่สามารถแทนลำดับราย slot ได้ด้วยไฟล์เดียว
  ต่อกลุ่ม — ถ้าต้องการตรงเป๊ะราย slot ต้องแยกกลุ่มหรือยอมรวม `hair_front` กับ `face`

### unknown (ยังไม่รู้ / ยังไม่ได้ทดสอบ)

- Qwen-Image-Edit จะได้ผลดีขึ้นจริงหรือไม่เมื่อใช้ชุดนี้ (ยังไม่ได้ทดลองสร้างภาพจริง)
- `SpineUiActor` (SkeletonGraphic บน Canvas) ให้พิกเซลเหมือน `SkeletonAnimation` ที่ใช้ใน tool นี้หรือไม่
  ในทุกกรณี — tool ใช้ `SkeletonAnimation` เพราะไม่ต้องพึ่ง Canvas และ material ของ atlas ถูกแทนที่ด้วย
  material ชั่วคราว (ไม่บันทึกเป็น asset) ค่า alpha ที่ได้จึงควรเท่ากัน แต่ยังไม่ได้เทียบกับ UI renderer
- พฤติกรรมเมื่อ atlas มีหลายหน้า/material หลายตัว (skeleton นี้มีหน้าเดียว) — โค้ดรองรับหลาย material
  แต่ยังไม่ได้ทดสอบกับ skeleton ที่มีหลาย atlas
- ยังไม่ได้ทดสอบกับ id อื่น (`1110011`, `410041_1`, `460011_1`, `570036`) — ควรลองก่อนใช้กับตัวละครอื่น
- ไม่มีการประกอบกลับเข้า Spine (สร้างสกิน/`SkeletonDataAsset` ใหม่) ตามขอบเขตของรอบนี้
