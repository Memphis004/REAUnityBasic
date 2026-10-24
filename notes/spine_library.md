# คลังอ้างอิง Spine (`extracted/spine_all`) + นำเข้า body_1110901

วันที่: 2026-10-24
Unity project: `C:\Workspace\REA\my_clone` (2021.3.45f2)
เกม (อ่านอย่างเดียว): `C:\Workspace\REA\game_copy` — ไม่มีการแก้ไฟล์เกม และไม่มีการคัดลอก DLL
คลังดิบ: `C:\Workspace\REA\extracted\spine_all` (นอก `my_clone` เท่านั้น)
ไฟล์ที่นำเข้า Unity: `Assets/_LocalReference/Spine/body_1110901/` เท่านั้น

---

## 0. สรุปสั้น

| รายการ | ค่า |
|---|---|
| SkeletonData ที่สแกนพบ | **6,614 rows** (จาก 6,678 bundle, 2 root) |
| id ไม่ซ้ำ | **6,560** (เข้าเกณฑ์ดึง 6,558 — อีก 2 id ไม่มี skeleton TextAsset) |
| ดึงสำเร็จ | **6,555** |
| ดึงล้มเหลว | **3** (บันทึกใน `errors.csv`) + 2 rows ที่สแกนไม่พบ skeleton TextAsset |
| ขนาดรวม | **4.5 GB บนดิสก์** (`du`; ผลรวมตามแถว = 4.22 GB เพราะ shared texture นับซ้ำตามการอ้างอิง) |
| atlas pages | 6,703 หน้า |
| texture ที่ใช้ร่วมกัน (เก็บชุดเดียว) | 126 ไฟล์ / 295 การอ้างอิง |
| thumbnail | 6,555 ไฟล์ (`_thumbs/`) |
| เวอร์ชัน Spine | **3.8.75 ทั้งหมด 6,558 ตัว** (ไม่ใช่ 3.8.x = 0) |
| หมวดใน catalog | ยืนยันจาก `dbbodyconfig` **2,746** · `dbcomponentcfg` **1,402** · เดาจาก bundle **2,410** |
| นำเข้า Unity รอบนี้ | **5 ตัว**: `body_1110901` + `460011_1`, `410041_1`, `1110011`, `570036` |
| Library ใน Demo | ปุ่ม **Next Library** สลับทั้ง 5 ตัวในช่อง 25% เดียวกัน (ตัวละ 1 ตัวที่มองเห็น) |
| Play checks | **PASS** (errors = 0) |

> หมายเหตุสำคัญ: index เดิม (`extracted/spine_id_index.json`) **ไม่ครบ** เพราะกรองเฉพาะ id ที่ขึ้นต้น
> ด้วย `11`/`21` (1,122 rows) ทำให้ `body_1110901` หายไปทั้งที่ bundle มีอยู่จริง จึงสแกนใหม่ทั้งหมดในรอบนี้

---

## 1. ก่อนดึง (ผลสำรวจ + ประตู 10 GB)

### 1.1 แหล่งข้อมูลที่สแกน

| root | bundle | skeleton rows |
|---|---:|---:|
| `game_copy\Windows\character\spinelib` (hot-update) | 6,648 | 6,584 |
| `game_copy\Immortal Clan Grandmastery_Data\StreamingAssets\Windows\character\spinelib` | 30 | 30 |

ไม่พบโฟลเดอร์ `StreamingAssets/aa` ในเกมนี้ (สแกนจาก `character/spinelib` ทั้งสองที่ตามที่โจทย์อนุญาต)
ทั้งสองชุด **ไม่ทับซ้อนชื่อกัน** (30 ตัวใน StreamingAssets ไม่มีใน hot-update)

### 1.2 จำนวนและขนาด

- SkeletonData rows: 6,614 / id ไม่ซ้ำ 6,558 (duplicate id 54 คู่ — เก็บตัวที่ skeleton ใหญ่สุด)
- ประเมินจากตัวอย่าง 40 skeleton: PNG จริง = **0.188 × ขนาด raw RGBA**
  → ประมาณการรวม **~3.9 GB** (skeleton 633 MB + atlas 15 MB + PNG ~3.3 GB)
- พื้นที่ว่างไดรฟ์ C: **160 GB** → ไม่เกินประตู 10 GB จึงดำเนินการต่อโดยไม่ต้องถาม
- ผลจริง: **4.5 GB บนดิสก์** (ผลรวมตามแถว skeleton 4.22 GB = texture 3.68 GB + skeleton/atlas 453 MB;
  texture ที่ใช้ร่วมกันถูกนับซ้ำในผลรวมตามแถว) — ใกล้ประมาณการ

### 1.3 เครื่องมือที่เพิ่ม (ทุกไฟล์อยู่ใน `extracted/`, ถูก gitignore)

| ไฟล์ | ใช้ทำอะไร |
|---|---|
| `scan_spine_all.py` | สแกนทุก bundle → `spine_all_index.json` (1,122 → 6,614 rows) |
| `scan_textures.py` | index Texture2D ทุกตัว → `spine_tex_index.json` (6,891 ชื่อ) |
| `extract_spine_all.py` | ดึง skeleton/atlas/texture + thumbnail → `spine_all/` |
| `build_spine_catalog.py` | รวม manifest + runtime stats → catalog/errors/index.html |

---

## 2. การดึงและการจัดวาง

```
extracted/spine_all/
├─ <id>/
│  ├─ <id>.skel.bytes        binary skeleton (ทุกตัวเป็น binary; ไม่มี .json)
│  ├─ <id>.atlas.txt         atlas (LF)
│  ├─ <page>.png             ชื่อไฟล์ตรงกับบรรทัด page ใน atlas
│  └─ source.json            ต้นทาง/ขนาด/hash/material ของเกม
├─ _shared/textures/<page>.png   texture ที่ byte-identical และชื่อ page ตรงกัน ใช้ร่วมกัน
├─ _thumbs/<id>.png          ภาพตัวอย่างหน้า atlas แรก (≤256px)
├─ _extract_manifest.json    ข้อมูลดิบของทุก skeleton
├─ _runtime_stats.json       ผล parse ด้วย runtime 3.8 (bones/slots/skins/anims)
├─ catalog.csv / catalog.json
├─ errors.csv
└─ index.html
```

### 2.1 กติกาการตั้งชื่อ / การรวม texture

- skeleton ทุกไฟล์ตั้งชื่อตาม id (ไม่ใช้ชื่อไฟล์เดิมในเกม) — `<id>.skel.bytes`
- atlas — `<id>.atlas.txt`
- texture — ใช้ชื่อ page ตรงตัวจาก atlas เช่น `body_1110901.png`
- texture ที่ **(ชื่อ page, sha256) เหมือนกันทุก byte** เก็บชุดเดียวที่ `_shared/textures/`
  และทุก skeleton ที่ใช้จะชี้ path เดียวกันใน `source.json`/catalog → ประหยัด 126 ไฟล์ (295 reference)
- texture ที่ชื่อซ้ำแต่เนื้อหาต่างกัน **ไม่รวม** (เก็บแยกในโฟลเดอร์ของตัวเอง)

### 2.2 texture ที่ไม่ได้อยู่ใน bundle เดียวกับ skeleton

บางตัวเก็บไว้ที่ `sharedtextures/<id>.ab` (เช่น `body/1110701_1/sharedtextures/`) เครื่องมือจึงสร้าง
index texture ทั้ง spinelib และเลือกตามลำดับ: bundle เดียวกัน → sibling `sharedtextures` →
path ใกล้สุด โดยตรวจ `size:` ของ page ใน atlas ประกอบ

### 2.3 งานที่ล้มเหลว (ไม่มีการข้ามเงียบ — อยู่ใน `errors.csv`)

| id | stage | เหตุผล |
|---|---|---|
| `2113019_21` | scan | bundle ไม่มี TextAsset `.skel`/`.json` (มีแต่ SkeletonData) |
| `mmxl` | scan | bundle ไม่มี TextAsset `.skel`/`.json` |
| `1110201_s` | extract | page `1110201_s2.png` ไม่มี Texture2D (ในเกมไม่มีชื่อนี้เลย) |
| `302563` | extract | page `3025632.png` ไม่มี Texture2D |
| `W_wxg_zjm` | extract | page `W_wxg_zjm2.png` ไม่มี Texture2D |

ค้นทั้ง `game_copy/Windows` และ `StreamingAssets` ด้วยชื่อ texture ทั้ง 3 แล้ว **ไม่พบไฟล์/bundle ชื่อนั้น**
จึงสรุปว่าเป็นข้อมูลที่เกมไม่ได้ ship หน้านั้นมา (ไม่ใช่ path ที่หาไม่เจอ)

นอกจากนี้มี 6 ตัวที่ atlas ไม่มี region ที่ skeleton อ้าง (ข้อมูลเกมไม่ครบ):
`R_rukou`, `tzsb`, `xzlsz`, `X_xjyz_czbz_1_b`, `X_xjyz_czbz_2`, `X_xy_ddxq`
→ parse ด้วย lenient loader เพื่อให้ได้ bones/slots/skins/anims ครบ และบันทึกเป็น
`runtime-parse-warning` ใน `errors.csv` (ไม่ได้ปิดบัง)

---

## 3. แคตตาล็อก

`catalog.json` (12.9 MB) และ `catalog.csv` (1.4 MB, UTF-8 BOM) มี 6,558 แถว
ฟิลด์ต่อ skeleton:

`id, originalName (ชื่อ SkeletonDataAsset เดิม), bundle, assetBundleName, sourceRoot, format,
spineVersion, versionOk/versionFlag, bones, slots, skins + skinNames[], animations + animationNames[],
atlasPages, pages[] (page/ขนาด/bytes/sha256/path/shared), textureSize, textureBytes,
fileBytes, fileBytesWithTextures, group, groupMeaning, groupConfidence, groupBasis, family,
category, categoryGuessed, categoryBasis, configEvidence{dbbodyconfigRows, dbcomponentcfgRows,
bodyConfigIds[], followSlots[]}, thumb, status, error, alternates[]`

- **กลุ่มตามรหัสนำหน้า** (`group`): ตัวเลข 2 หลักแรก หรือ `other`
  `other 3,892 / 11 747 / 30 379 / 21 353 / 57 352 / 51 167 / 41 140 / 77 136 / 52 103 / 82 61 / 45 56 / 58 44 / 46 36 / 31 18 / 71 12 / 76 12 / 54 11 / 00 9 / 60 8 / 42 6 / 74 4 / 73 3 / 22 2 / 43 2 / 44 2 / 12 1 / 24 1 / 26 1`
  ทุกกลุ่มมี `groupMeaning` + `groupConfidence` และตารางหลักฐานอยู่ใน `prefixGroups` (ดูข้อ 7)
- **family** (โทเคนนำหน้า): `11 747, body 720, 30 379, Body 363, 21 353, 57 352, face 277, hair 259,
  X 203, 51 167, ...` — กลุ่มที่ไม่ขึ้นต้นด้วยตัวเลขรวม 3,892 ตัว
- **หมวด (`category`)**: เอา **id ไปเทียบ config จริงก่อน** — ถ้า `dbbodyconfig.spineAb`/
  `s_spineAb` อ้าง id นั้น จะใช้ชนิดจากแถว config (`configEvidence.bodyConfigIds`); ถ้าไม่พบ
  จึงใช้ `dbcomponentcfg.spineAb` (ชิ้นส่วนตัวละคร: face/hair/hat/accessory); ถ้าไม่พบทั้งคู่
  จึงเดาจากโฟลเดอร์ bundle และติด `categoryGuessed: true`
  → `dbbodyconfig 2,746` + `dbcomponentcfg 1,402` + `bundle (guess) 2,410` = 6,558
  หมวดที่ได้: `UI element 2,127`, `scene prop / building 1,094`,
  `character component (half body - disciple) 466`, `full body - costume 281`,
  `character component (face) 260`, `character component (hair) 241`,
  `scene prop (gubao) 223`, `monster 220`, `full body - immortal/demon 211`,
  `character component (half body - NPC) 134`, `half body - NPC 131`, `half body - disciple 122`,
  `half body - player 110`, `character component (hat) 102` …
- **ธงเวอร์ชัน**: ทุกตัวเป็น `3.8.75` → `ok-3.8.x = 6,558`, `UNSUPPORTED-non-3.8 = 0`
  (ยังมีฟิลด์นี้ไว้เพราะ runtime 3.8 อ่านข้อมูลนอกสาย 3.8.x ไม่ได้)
- `alternates` เก็บ bundle อื่นที่ ship id เดียวกัน (54 id) — เก็บ skeleton ที่ใหญ่สุดเป็นตัวหลัก

ตัวอย่างที่ตรวจซ้ำ:

| id | bones | slots | skins | anims | page | texture |
|---|---:|---:|---:|---:|---:|---|
| `body_1110901` | 64 | 23 | 8 | 23 | 1 | 1152×80 |
| `1113002_1` | 77 | 25 | 4 | 24 | 1 | 364×80 |
| `1123011` | 25 | 15 | 15 | 6 | 1 | 1016×272 |
| `2110101` | 1 | 19 | 11 | 1 | 1 | 1252×1028 |

(`2110101` มี bone เดียวจริง เพราะเป็น portrait ที่ใช้ slot/skin อย่างเดียว)

### 3.1 ตัวกรองหมวด + หลักฐาน config (ใน `index.html`)

- **ตัวกรองหมวด** — `<select id="category">` สร้างตัวเลือกจากค่า `category` ที่ไม่ซ้ำกันใน `DATA`
  (39 หมวด) และเทียบค่าตรงตัว (`s.category === c`) ใช้ร่วมกับตัวกรองอื่นแบบ AND ทั้งหมด:
  รหัสนำหน้า (`#group`), เวอร์ชัน (`#version`), ชื่อ skin/animation (`#nameq`), คำค้น (`#q`),
  และหลักฐานของหมวด (`#evidence`)
- **ตัวกรองหลักฐาน config** — `<select id="evidence">`

| ตัวเลือก | เงื่อนไข | จำนวนแถว |
|---|---|---:|
| `All category sources` | ไม่กรอง | 6,558 |
| `confirmed by config` | `categoryGuessed = false` | **4,148** (dbbodyconfig 2,746 + dbcomponentcfg 1,402) |
| `bundle-only guess` | `categoryGuessed = true` | **2,410** |

- `confirmed by config` ไม่ได้แยกว่าเป็น `dbbodyconfig` หรือ `dbcomponentcfg` — ทั้งสองนับเป็น
  "มีหลักฐานจาก config" ถ้าต้องการดูแยกรายตาราง ให้เปิดแผงรายละเอียดของแถวนั้น
  หัวข้อ **Config evidence** แสดง `dbbodyconfig rows`, `dbcomponentcfg rows` และ `followSlot`
- แถวที่เดาจาก bundle แสดงป้าย `guess` ท้ายหมวด; แถวที่ยืนยันแล้วไม่แสดงป้าย
  (ในแผงรายละเอียดขึ้น `(config-confirmed)`)
- ตัวนับบนหัวตารางเป็น `N of 6,558 skeletons` ตามผลหลังกรองทุกตัว (ก่อนจำกัดแสดง 2,500 แถว)
- ตัวอย่างการกรองร่วม: หมวด `monster` + `confirmed by config` = 113 แถว;
  หมวด `UI element` + `bundle-only guess` = 1,318 แถว
- ตัวกรองทำงานฝั่ง client จาก `DATA` ก้อนเดียว — ไม่แก้ไข `catalog.json`/`catalog.csv`

### 3.2 ตารางสรุปจำนวนต่อหมวด + ที่มาของหลักฐาน

ลำดับการตัดสินหมวด: `dbbodyconfig.spineAb`/`s_spineAb` → `dbcomponentcfg.spineAb` → เดาจาก path
ของ bundle; คอลัมน์ **bundle** คือแถวที่ `categoryGuessed: true` ทั้งหมด (นับจาก `catalog.json`
6,558 แถว)

| หมวด (`category`) | รวม | dbbodyconfig | dbcomponentcfg | bundle (guess) |
|---|---:|---:|---:|---:|
| `UI element` | 2,127 | 809 | 0 | 1,318 |
| `scene prop / building` | 1,094 | 727 | 0 | 367 |
| `character component (half body - disciple)` | 466 | 0 | 466 | 0 |
| `full body - costume` | 281 | 254 | 0 | 27 |
| `character component (face)` | 260 | 0 | 260 | 0 |
| `character component (hair)` | 241 | 0 | 241 | 0 |
| `scene prop (gubao)` | 223 | 12 | 0 | 211 |
| `monster` | 220 | 113 | 0 | 107 |
| `full body - immortal/demon` | 211 | 195 | 0 | 16 |
| `character component (half body - NPC)` | 134 | 0 | 134 | 0 |
| `half body - NPC` | 131 | 88 | 0 | 43 |
| `half body - disciple` | 122 | 89 | 0 | 33 |
| `half body - player` | 110 | 24 | 0 | 86 |
| `character component (hat)` | 102 | 0 | 102 | 0 |
| `full body - special` | 99 | 87 | 0 | 12 |
| `full body - NPC` | 92 | 87 | 0 | 5 |
| `character component (UI element)` | 90 | 0 | 90 | 0 |
| `scene prop (sundrise)` | 84 | 14 | 0 | 70 |
| `cutscene (liandong)` | 75 | 49 | 0 | 26 |
| `character component (accessory)` | 64 | 0 | 64 | 0 |
| `disciple special` | 63 | 52 | 0 | 11 |
| `scene prop` | 55 | 23 | 0 | 32 |
| `full body - female` | 33 | 30 | 0 | 3 |
| `character component (half body - player)` | 33 | 0 | 33 | 0 |
| `full body` | 28 | 27 | 0 | 1 |
| `monster (kongzhan)` | 27 | 26 | 0 | 1 |
| `full body - other` | 26 | 15 | 0 | 11 |
| `half body` | 16 | 0 | 0 | 16 |
| `full body - variant` | 15 | 12 | 0 | 3 |
| `scene prop (junzheng)` | 9 | 0 | 0 | 9 |
| `weapon` | 8 | 8 | 0 | 0 |
| `character component (scene prop / building)` | 5 | 0 | 5 | 0 |
| `character component (cutscene (liandong))` | 4 | 0 | 4 | 0 |
| `mount` | 3 | 3 | 0 | 0 |
| `scene prop (junzhen)` | 2 | 2 | 0 | 0 |
| `scene prop (dengluye)` | 2 | 0 | 0 | 2 |
| `character component (weapon)` | 1 | 0 | 1 | 0 |
| `character component (emote / expression)` | 1 | 0 | 1 | 0 |
| `character component (full body - female)` | 1 | 0 | 1 | 0 |
| **รวม** | **6,558** | **2,746** | **1,402** | **2,410** |

อ่านตาราง:

- ทุกหมวดที่ขึ้นต้นด้วย `character component (...)` มาจาก `dbcomponentcfg` ล้วน (ชิ้นส่วนที่เกาะกับ
  body) — ไม่มีแถวไหนเดาจาก bundle
- `UI element` และ `scene prop / building` เป็นสองหมวดใหญ่สุด และมีเดาจาก bundle มากสุด
  (1,318 / 367) เพราะ artwork UI จำนวนมากไม่ถูกอ้างใน config
- `scene prop (gubao)` เดาจาก bundle ถึง 211/223 เพราะแถว config ของกลุ่ม `30` (90 แถว)
  ถูกใช้เป็นชิ้นส่วน UI (`character component (UI element)`) ไม่ใช่ตัว gubao — ทั้งคลังมีเพียง 12 แถว
  config ที่จัดเป็น gubao
- หมวดที่เหลือ 1–9 แถวเป็นของเฉพาะกิจ (`scene prop (junzheng)`, `weapon`, `mount`, …)
- หลักฐานระดับ **รหัสนำหน้า** (กลุ่ม `11`/`21`/`30`/…) อยู่ในข้อ 7.2 — ตารางนี้เป็นระดับ `category`
  ซึ่งเป็นค่าที่ตัวกรองใน `index.html` ใช้

---

## 4. thumbnail + index.html

- `_thumbs/<id>.png` = หน้า atlas แรก ย่อด้านยาวไม่เกิน **256 px** (6,555 ไฟล์)
- `index.html` เป็นไฟล์เดียว self-contained (CSS/JS inline, รูปอ้าง `_thumbs/...` แบบ relative)
  **ไม่มี http/https หรือ CDN ใด ๆ** (ตรวจแล้ว 0 reference)
- ตาราง: thumbnail, id + bundle, หมวด (+ `guess` ถ้าเดา) + ความหมายกลุ่ม, bones/slots, skins/anims,
  atlas pages, Spine version, size, ตัวอย่างชื่อ animation
- ค้นหา (id/bundle/category/ความหมายกลุ่ม) + ช่อง **ชื่อ skin/animation** (พิมพ์บางส่วนของชื่อได้)
- กรองตาม **รหัสนำหน้า**, **หมวด** (`#category`, 39 หมวด), เวอร์ชัน, และ **หลักฐานของหมวด**
  (`confirmed by config` 4,148 / `bundle-only guess` 2,410) — รายละเอียดในข้อ 3.1
- ตัวกรองทั้งหมดใช้ร่วมกันแบบ AND; ตารางสรุปจำนวนต่อหมวดพร้อมที่มาของหลักฐานอยู่ในข้อ 3.2
- **คลิกแถว → แผงรายละเอียด** แสดงสกินทั้งหมด, animation ทั้งหมด, หมวด + `categoryBasis`,
  จำนวนแถว config + `followSlot`, ความหมาย/ความมั่นใจของกลุ่ม, atlas pages พร้อมขนาด, alternates
- deep link เปิดรายละเอียดตรงตัว: `index.html#body_1110901`
- ตรวจด้วย Chrome headless (`--headless=new --screenshot`) แล้วเห็นตาราง/รูป/ตัวกรอง/แผงรายละเอียดทำงานจริง
- ตารางแสดงสูงสุด 2,500 แถวต่อครั้งเพื่อความลื่น (แจ้งบนหัวตาราง) — ใช้ตัวกรอง/ค้นหาเพื่อเข้าถึงทั้ง 6,558

เปิดดู: ดับเบิลคลิก `extracted\spine_all\index.html`

---

## 5. เครื่องมือใน Unity

ไฟล์: `my_clone/Assets/Editor/SpineLibraryTool.cs` (ไม่แตะ `SpineUiActor`)

### 5.1 นำเข้า

- เมนู: **Tools/Local Reference/Import Spine By Id** (หน้าต่างกรอก id คั่นด้วย comma)
- เรียกจากโค้ด/batch: `MyClone.EditorTools.SpineLibraryTool.ImportById("body_1110901,1123011")`
- อ่านจาก `extracted/spine_all` (คำนวณจาก `Application.dataPath/../..` — นอก Assets เสมอ)
- คัดลอกไป `Assets/_LocalReference/Spine/<id>/`: skeleton, atlas, PNG ทุก page
  (ถ้า page อยู่ใน `_shared/textures` จะคัดลอกมาจากที่นั่น)
- สร้าง/อัปเดต: `<id>_Atlas.asset`, `<id>_SkeletonData.asset` (scale 0.01, defaultMix 0.2),
  `<id>_Material.mat` (+ `_Material_<page>` ถ้าหลายหน้า), `<id>_Material_Gray.mat`
- import settings เหมือนจิบิตัวก่อน: Texture Type Default, alphaIsTransparency = false,
  mipmap off, sRGB on, Clamp, Bilinear, NPOT none, Uncompressed, max 2048
- material ใช้ shader `Spine/SkeletonGraphic`; PMA ตามหลักฐาน material ของเกม
  (`_StraightAlphaInput = 0` → ตั้ง 0 และปิด keyword `_STRAIGHT_ALPHA_INPUT`)
- รายงานผลใน Console: version, bones/slots/skins/animations, จำนวน page, alpha mode + หลักฐาน,
  path ของ asset; ถ้า id ไม่มีในคลังจะ error ชัดเจนพร้อม id ใกล้เคียง เช่น
  `'body_9999999' is not in the library. Closest ids: ...`
- นำเข้าหลาย id พร้อมกันได้; ตัวที่ล้มเหลวไม่หยุดตัวอื่น และสรุปท้ายด้วย `imported X/Y`

### 5.2 Dump runtime stats (ใช้ทำแคตตาล็อก)

- เมนู: **Tools/Local Reference/Spine Library/Dump Runtime Stats**
- เรียก: `SpineLibraryTool.DumpRuntimeStats(<path>/_runtime_stats.json)`
- parse ทุก `<id>.skel.bytes` ด้วย `SkeletonBinary` + atlas text (ไม่สร้าง asset, ไม่อิมพอร์ตเข้า Assets)
- 6,555/6,555 ตัวที่อยู่ในคลัง parse สำเร็จ (ใช้เวลา ~12 วินาที)

---

## 6. นำเข้า `body_1110901` + Demo

### 6.1 ผลนำเข้า (observed)

```
Assets/_LocalReference/Spine/body_1110901/
  body_1110901.skel.bytes (327,570 B)
  body_1110901.atlas.txt  (5,414 B)
  body_1110901.png        (107,123 B, 1152x80)
  body_1110901_Atlas.asset
  body_1110901_SkeletonData.asset
  body_1110901_Material.mat / _Material_Gray.mat
```

ตรวจผ่าน Unity MCP:

```
ver=3.8.75 bones=64 slots=23 skins=8 anims=23
skins=[default, body_1110901_1..3, face_1110901_1, head_1110901_1..3]
anims=[attack1..8, dead, hit, idle1..5, jump1..3, run, stand, walk]
texture: Default, alphaIsTransparency=False, mips=False, Clamp, Bilinear, Uncompressed, sRGB=True
material: Spine/SkeletonGraphic, straight=0, keyword _STRAIGHT_ALPHA_INPUT=False, scale=0.01
```

### 6.2 Demo (`Assets/_LocalReference/Demo.unity`)

- `LocalReferenceDemoBuilder` สร้าง actor ตัวที่ 6 ด้วย `SpineActorFactory.CreateUi`
  (ชื่อ `TreeFarm_body_1110901`) ที่ช่อง 25% ระหว่าง chibi กับ third, บังคับ animation `stand` loop
- `SpineDemoController` เพิ่ม `treeFarm` เข้า Next Skin / Next Animation / Flip X / Toggle Gray
  และ HUD บรรทัด `CHIBI 1110901 (Tree Farm): bones 64 slots 23 skins 8 anims 23 | ... | stand`
- Play checks (รันจริงใน play mode ผ่าน MCP): **PASS, errors = 0** — ตรวจความสูง 24.5%,
  `stand` ตอน Start, ปุ่มทั้งสี่มีผลกับ actor นี้, ผิว/อนิเมชัน/กลับด้าน/grayscale เปลี่ยนจริง
- ภาพจาก play mode: ตัวละครแสดงสีครบ (ไม่ขาว/ไม่เพี้ยน) และ layout ไม่ทับตัวอื่น
  → ไม่ใช่ปัญหา shader/PMA จึงไม่ได้แก้ shader; ค่า PMA ยังตรงกับ material เกม

### 6.3 Library ใน Demo (5 ตัว + Next Library)

- `LocalReferenceDemoBuilder` โหลด 5 ตัวจาก `Assets/_LocalReference/Spine/<id>/`:
  `body_1110901` (Tree Farm), `460011_1` (มอนสเตอร์เห็ด), `410041_1` (มอนสเตอร์),
  `1110011` (ตัวปีศาจ/ควัน), `570036` (ต้นไม้/สิ่งก่อสร้าง)
- ทุกตัวอยู่ช่อง 25% เดียวกัน (`sideFraction = 0.30`) ใช้ `stand` และแสดงทีละตัวด้วย alpha;
  ตัวที่กว้างถูกจำกัดความกว้าง `0.13 × canvas` เพื่อไม่ทับ chibi/third
- ปุ่ม **Next Library** วน `body_1110901 → 460011_1 → 410041_1 → 1110011 → 570036 → กลับ`
  และ HUD บรรทัด `LIBRARY n/5 <id>` แสดงตัวที่กำลังดู
- `Next Skin` / `Next Animation` / `Flip X` / `Toggle Gray` มีผลกับทุกตัวใน library ด้วย
  (รวมตัวที่ซ่อนอยู่ จึงพร้อมทันทีเมื่อสลับมาถึง)
- Play checks วนครบทั้ง 5 ตัว: ตรวจ index, mesh, alpha ของตัวที่เห็น/ตัวก่อนหน้า, animation,
  และเขียนภาพ `demo_library_460011_1.png` → **PASS, errors = 0**
- ข้อจำกัด: ภาพที่จับใน *เฟรมเดียวกับที่กดสลับ* อาจยังมีตัวเก่าค้างหนึ่งเฟรม (Canvas อัปเดตในเฟรมถัดไป);
  การแสดงผลจริงหลังเฟรมนั้นถูกต้อง

---

## 7. รูปแบบรหัส (id)

### 7.1 Observed (โครงสร้าง id)

- id = ชื่อ SkeletonDataAsset ตัด `_SkeletonData` (เช่น `body_1110901_SkeletonData` → `body_1110901`)
- 6,558 id ประกอบด้วย:
  - ขึ้นต้นด้วยตัวเลข 2,666 ตัว แบ่งเป็น `11` 747, `30` 379, `21` 353, `57` 352, `51` 167,
    `41` 140, `77` 136, `52` 103, `82` 61, `45` 56, `58` 44, `46` 36, `31` 18, `76` 12,
    `71` 12, `54` 11, `00` 9, `60` 8, `42` 6, `74` 4, `73` 3, `22` 2, `43` 2, `44` 2,
    `12` 1, `24` 1, `26` 1
  - ขึ้นต้นด้วยตัวอักษร 3,892 ตัว: `body 720, Body 363, face 277, hair 259, X 203, hat 109,
    M 94, S1 90, L 86, Z 83, accessory 82, W 77, S 75, T 74, Y 69, D 63, qpk 60, J 49,
    C 40, world 38, ...`
- suffix ที่พบซ้ำ: `_s` (ตัวย่อ/สำรอง), `_1.._5` (variant), `_bg` (ฉากหลังของ cutscene),
  `_tu`, `_test`; บาง id มี `_01`, `_21` ต่อท้าย
- bundle ต้นทางบอกหมวดได้ชัดกว่า id เช่น `body/*`, `body_shizhuang/*`, `halfbody_dizi/*`,
  `halfbody_npc_singleskeleton/*`, `ui*/*`, `build*/*`, `monster/*`, `liandong/*`, `weapon/*`
- 54 id ปรากฏในมากกว่า 1 bundle (เช่น `113103_1` อยู่ทั้ง `body_special` และ `body_xianmo`)
  และบางคู่เป็นคนละขนาด/คนละเนื้อหา → เก็บตัวใหญ่สุด + `alternates`

### 7.2 ยืนยันกับ config (วิธี + ผล)

วิธี: เอา id ของทุก skeleton ไปเปิดใน `dbbodyconfig` (ฟิลด์ `spineAb`/`s_spineAb`) และ
`dbcomponentcfg` (ฟิลด์เดียวกัน) — ถ้า config อ้าง id นั้นจริง = มีหลักฐานว่า id นี้ถูกใช้เป็น
อะไรในเกม แล้วจึงสรุปความหมายของ **กลุ่มนำหน้า** จากเสียงส่วนใหญ่ของสมาชิกในกลุ่ม
(ไม่ใช่การเดาจากตัวเลข)

ความมั่นใจที่ใช้ใน catalog:

| ค่า | ความหมาย |
|---|---|
| `confirmed-majority` | สมาชิกส่วนใหญ่ถูก config อ้าง และชนิดที่ config ใช้ตรงกับความหมายของกลุ่ม |
| `mixed` | กลุ่มเดียวมีหลายชนิดปนกันจริง (เช่น 30 = gubao + exploration UI + daobing) |
| `inferred` | มีเพียง bundle path/ตัวอย่างไม่กี่ตัวหนุน |
| `unknown` | ไม่มีแถว config และ bundle ไม่ชัด |

กลุ่มตัวเลข (จำนวน / อ้างใน dbbodyconfig / dbcomponentcfg):

| กลุ่ม | n | ความหมาย | ความมั่นใจ | body / comp | ตัวอย่าง |
|---|---:|---|---|---:|---|
| `11` | 747 | ตัวละครเต็มตัว (ชุด/immortal/demon/NPC/liandong) | confirmed-majority | 677 / 25 | `1110011`, `1110102_1` |
| `30` | 379 | ฉาก/UI prop (gubao, exploration, daobing) | mixed | 47 / 90 | `302108`, `302109` |
| `21` | 353 | ตัวละครครึ่งตัว / portrait | confirmed-majority | 254 / 88 | `2110101` |
| `57` | 352 | สิ่งก่อสร้าง / ของตกแต่งฉาก | confirmed-majority | 290 / 0 | `570036` |
| `51` | 167 | สิ่งก่อสร้าง | confirmed-majority | 155 / 0 | `510011` |
| `41` | 140 | มอนสเตอร์ | confirmed-majority | 93 / 0 | `410041_1` |
| `77` | 136 | สิ่งก่อสร้าง / UI prop | mixed | 105 / 0 | `771001` |
| `52` | 103 | สิ่งก่อสร้าง | confirmed-majority | 91 / 0 | `520011` |
| `82` | 61 | สิ่งก่อสร้าง / UI (ปนกัน) | mixed | 42 / 0 | `820153` |
| `45` | 56 | UI ตู้ปลา (`ui_yugang`) | confirmed-majority | 56 / 0 | `450000` |
| `58` | 44 | สิ่งก่อสร้าง | confirmed-majority | 34 / 4 | `580001` |
| `46` | 36 | มอนสเตอร์ | confirmed-majority | 35 / 0 | `460011_1` |
| `31` | 18 | ฉาก gubao | confirmed-majority | 12 / 0 | `315001` |
| `71` | 12 | สิ่งก่อสร้าง - ลับแดน (`build_mijing`) | confirmed-majority | 12 / 0 | `711060` |
| `76` | 12 | สิ่งก่อสร้าง - ลับแดน | confirmed-majority | 11 / 1 | `760000` |
| `54` | 11 | สิ่งก่อสร้าง | confirmed-majority | 11 / 0 | `540000` |
| `00` | 9 | ฉาก/UI junzheng | unknown (ไม่มีแถว config) | 0 / 0 | `001`, `002` |
| `60` | 8 | ฉาก sundrise | confirmed-majority | 8 / 0 | `607113` |
| `42` | 6 | มอนสเตอร์ | confirmed-majority | 6 / 0 | `420011_1` |
| `74` | 4 | สิ่งก่อสร้าง - ลับแดน | confirmed-majority | 4 / 0 | `741131` |
| `73` | 3 | สิ่งก่อสร้าง - ลับแดน | confirmed-majority | 3 / 0 | `731064` |
| `22` | 2 | ครึ่งตัวสำหรับ cutscene | confirmed-majority | 2 / 0 | `22113615_01` |
| `43` | 2 | มอนสเตอร์ | inferred (ไม่มีแถว config) | 0 / 0 | `430021_1` |
| `44` | 2 | NPC body / มอนสเตอร์ (ปนกัน) | mixed | 2 / 0 | `440011` |
| `12` | 1 | UI element | inferred | 1 / 0 | `12121212` |
| `24` | 1 | สิ่งก่อสร้าง - ลับแดน | confirmed-majority | 1 / 0 | `240127` |
| `26` | 1 | prop กิจกรรม duanwu | unknown | 0 / 0 | `26duanwu` |

ชื่อนำหน้าแบบมีคำ (family) ที่ config ยืนยันบทบาท:

| family | n | ความหมาย | หลักฐาน |
|---|---:|---|---|
| `body_` | 720 | โครง/ชิ้นส่วนครึ่งตัว ( disciple / NPC / player ) | `dbcomponentcfg` 667 แถว, `dbbodyconfig` 69 |
| `face_` | 277 | ใบหน้า (สกินเสริมของ portrait) | `dbcomponentcfg` 260 |
| `hair_` | 259 | ทรงผม | `dbcomponentcfg` 241 |
| `hat_` | 109 | หมวก | `dbcomponentcfg` 102 |
| `accessory_` | 82 | เครื่องประดับ | `dbcomponentcfg` 64 |
| `Body_` | 363 | ศิลป์ฉาก/สิ่งก่อสร้าง (ไม่ใช่ตัวละคร) | `dbbodyconfig` 0, `dbcomponentcfg` 0 → เดาจาก bundle |
| `X_` / `B_` / `M_` / `S1_` / `L_` / `Z_` / `W_` / `S_` | 203 / 15 / 94 / 90 / 86 / 83 / 77 / 75 | UI element (ป้าย/ไอคอน/กรอบ/ฉากย่อย) | ส่วนใหญ่ไม่มีแถว config; bundle `ui*` |
| `world_` | 38 | สิ่งก่อสร้างในลับแดน (`build_mijing`) | bundle + config |

ข้อสรุปสำคัญ: **รหัสนำหน้าเป็น "namespace" ของเนื้อหา ไม่ใช่รหัสชนิดที่ encode ตัวเลข**
(แกะตัวเลขแล้วบอกชนิดไม่ได้) — แต่ละกลุ่มมีชนิดเด่นที่ใช้จริงในเกม และมีข้อยกเว้นที่ปนอยู่
เช่น `11` มี monster/mount ปน ~70 ตัว, `30` ปน gubao/exploration UI/daobing, `82` ปน build/ui
จึงติด `mixed` ไว้ตรง ๆ แทนที่จะเหมารวมว่าเป็นชนิดเดียว

### 7.3 Inferred / Unknown

- `_s` = เวอร์ชันย่อ (skeleton เล็กกว่าและมักมี slot น้อยกว่า) — สังเกตจากขนาด/โครงสร้าง
- `halfbody_npc_singleskeleton` = half-body ที่มี skeleton เดี่ยว (1 bone) ใช้กับ portrait
- 3 texture page ที่หายไปมีอยู่ที่อื่นในเครื่องหรือถูกตัดออกจาก build (ค้นทั้งเกมแล้วไม่พบ)
- skeleton ที่ atlas ไม่ครบ 6 ตัวแสดงผลได้ถูกต้องในเกมหรือไม่ (ข้อมูล integrity ไม่ครบ)
- id ที่ไม่มีแถว config เลย (2,410 ตัว โดยเฉพาะ UI art) ยังเป็นหมวดเดาจาก bundle

---

## 8. ตรวจสอบกติกา

| กติกา | สถานะ |
|---|---|
| ห้ามคัดลอก DLL จากเกม | ไม่มี DLL จากเกมใน `my_clone` (DLL ที่มีอยู่เดิมคือ `Assets/Plugins/NuGet` ของ MCP plugin) |
| คลังดิบอยู่นอก Assets เท่านั้น | `extracted/spine_all` อยู่นอก `my_clone`; ไม่มีไฟล์คลังใน Assets |
| ไฟล์นำเข้าไปที่ `Assets/_LocalReference/Spine/<id>/` | 5 โฟลเดอร์ตามที่ผู้ใช้เลือก (body_1110901, 460011_1, 410041_1, 1110011, 570036) |
| ตรวจ `.gitignore` ก่อนคัดลอก | ครอบคลุมจริง: `extracted/*` (บรรทัด 160) และ `my_clone/Assets/_LocalReference/` (บรรทัด 175) — ตรวจด้วย `git check-ignore` ทั้งสอง path |
| ห้ามแก้ `SpineUiActor` | ไม่ได้แก้ (แก้เฉพาะ Demo builder/controller และเพิ่ม Editor tool) |
| ห้ามนำเข้า skeleton อื่น | นอกเหนือจาก `body_1110901` นำเข้าเพิ่มเฉพาะ 4 id ที่ผู้ใช้เลือกในรอบนี้ |

Console หลังตรวจ: **0 error** (ล้าง console แล้วรันซ้ำ; error ที่เห็นชั่วคราวมาจากการทดสอบ id
ที่ไม่มีอยู่จริงตามข้อ 5.1)

---

## 9. คำสั่งที่ใช้ซ้ำได้

```bash
# 1) สแกนใหม่ทั้งหมด (ถ้าต้องการรีเฟรช index)
python extracted/scan_spine_all.py
python extracted/scan_textures.py

# 2) ดึงทั้งหมด (หรือเฉพาะ id)
python extracted/extract_spine_all.py
python extracted/extract_spine_all.py --ids body_1110901

# 3) runtime stats + แคตตาล็อก
#    Unity: Tools/Local Reference/Spine Library/Dump Runtime Stats
#    หรือ MCP:
python extracted/unity_mcp.py --code artifacts/mcp_stats.cs
python extracted/build_spine_catalog.py

# 4) นำเข้า Unity
#    Unity: Tools/Local Reference/Import Spine By Id  ->  body_1110901
#    หรือ MCP: MyClone.EditorTools.SpineLibraryTool.ImportById("body_1110901")

# 5) สร้าง Demo ใหม่ (ถ้าต้องการ)
#    Unity: Tools/Local Reference/Build Demo Scene
```
