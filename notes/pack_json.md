# Pack JSON — disciple bodies

ส่งออกสเกเลตันใน `_LocalReference/Spine/` เป็นไฟล์ pack เดียวสำหรับเว็บ "Anatomy of a disciple":
ทุก slot ที่มี attachment ถูก render แยกชิ้นบน canvas เดียว/scale เดียว ครอปตามขอบ alpha
แล้วฝังเป็น data URI

- วันที่รัน: 2026-10-25 · Unity 2021.3.45f2 · project `C:\Workspace\REA\my_clone` · spine 3.8.75
- Tool: `my_clone/Assets/Editor/PackJsonExporter.cs`
  - เมนู `Tools/LocalReference/Export Pack JSON` (เลือก `SkeletonDataAsset` ก่อน)
  - batchmode ทีละตัว: `-executeMethod MyClone.EditorTools.PackJsonExporter.ExportDefault` (body_1110901)
  - batchmode ทั้งหมด: `-executeMethod MyClone.EditorTools.PackJsonExporter.ExportAll`
    (ไล่ทุกโฟลเดอร์ใต้ `Assets/_LocalReference/Spine/` ที่มี `*_SkeletonData.asset`)
- ผลลัพธ์: `extracted/web/<id>.pack.json` + `extracted/web/_<id>.pack_full_ref.png` (ภาพอ้างอิงไว้ตรวจ)
  + `extracted/web/packs.json` (สารบัญ pack — tool เขียนให้เองทุกครั้งที่ export)
  — อยู่นอก `Assets`, `git check-ignore` ผ่านด้วยกฎ `extracted/*`
- ตรวจ: `extracted/verify_pack.py <pack.json> <full_ref.png>` (reassembly เทียบ full ref)
- ดูด้วยตา: `extracted/web/index.html` (มี pack selector, เปิดผ่าน `python -m http.server`)

## 1. กลุ่ม (slot → group)

Tool หา map ตามลำดับนี้ (ต่อ id): `extracted/web/part_groups.json` →
`extracted/part_sheets/<id>/part_groups.json` → `extracted/ref_sheets/<id>/part_groups.json` →
`extracted/part_sheets/<id>/sheet_groups.json` → `extracted/ref_sheets/<id>/layers_groups.json`

| id | map ที่ใช้ | หมายเหตุ |
|---|---|---|
| body_1110901 | `extracted/part_sheets/body_1110901/sheet_groups.json` | ไฟล์เดิมของ part-sheet tool (เพิ่ม field `label` แล้ว) |
| 1110011, 410041_1, 460011_1, 570036 | `extracted/part_sheets/<id>/part_groups.json` | **เขียนใหม่ในรอบนี้** เพราะทั้ง 4 id ไม่มี map มาก่อน |

- `part_groups.json` schema เดียวกับ `sheet_groups.json`: `fallbackGroup` + `groups[].name|label|note|slots|matchPrefixes`
  — `slots` (ชื่อ slot ตรงตัว) ชนะ `matchPrefixes`, ที่ไม่เข้าใครเลยตกไป `fallbackGroup`
- **`label`** คือข้อความที่หน้าเว็บแสดง (tool ใช้ `groups[].label` ก่อน; ถ้าไม่มีจึงเดาจาก id)
  เพิ่มให้ครบทุกกลุ่มใน `sheet_groups.json` ของ body_1110901 แล้ว (14 กลุ่ม)
- ลำดับ `groups[]` ในไฟล์ = ลำดับบนลงล่างในหน้าเว็บ
- ถ้าไม่มี map เลย tool จะไม่ล้ม: ทุกชิ้นไปอยู่ `other` และเขียน warning บอก path ที่ควรสร้าง

### 1.1 กลุ่มของ 4 id ที่เขียนใหม่

| id | กลุ่ม (บน→ล่าง) | slot |
|---|---|---|
| 410041_1 | Head & ears / Torso / Left arm / Right arm / Left leg / Right leg / Ribbons | `tou`+`erduo` / `shenti` / `zuobi`+`zuoshou` / `youbi`+`youshou` / `zuotui`+`zuojiao` / `youtui`+`youjiao` / `piaodai1..3` |
| 460011_1 | Head & cap / Body / Left hand / Right hand / Leaves / Small mushroom | `tou` / `shenti` / `zuoshou` / `youshou` / `ye1..ye5` / `xiaomogu` |
| 1110011 | Smoke mass / Smoke wisp / Embers | `csy_8`+`csy_9` / `csy_2` / `csy_11`+`csy_10`+`csy_6`+`csy_5`+`csy_7`+`csy_1` |
| 570036 | Tree (กลุ่มเดียว) | `570036` |

- ตั้งชื่อกลุ่มจากชื่อ slot ของ binary (`shenti`=ตัว, `tou`=หัว, `erduo`=หู, `bi`=ต้นแขน, `shou`=มือ,
  `tui`=ขา, `jiao`=เท้า, `piaodai`=ผ้าคาด, `ye`=ใบ, `xiaomogu`=เห็ดเล็ก)
- **1110011 ไม่ใช่ตัวละคร**: ชื่อ slot เป็น `csy_*` ล้วน ไม่มีความหมายเชิงชิ้นส่วน จึงแบ่งตาม**ภาพ**ที่ render ออกมา
  (ไม่ใช่ anatomy): ควันก้อนหนา 2 ชิ้น = `smoke`, ริ้วบางกลางภาพ 1 ชิ้น = `smoke_wisp`, ลิ้นไฟสีชมพูเล็ก 6 ชิ้น = `embers`
  — ดูแผ่นเทียบ `artifacts/1110011_parts.png` / ภาพรวม `artifacts/1110011_composite.png`
- **570036 เป็นพร็อพต้นไม้** มี slot เดียว จึงมีกลุ่มเดียว
- `460011_1` มี slot `tou` ที่ไม่มี attachment ในสกินใดเลย → ถูกข้ามและรายงาน

## 2. ค่าที่ใช้ / วิธี render

| รายการ | ค่า |
|---|---|
| สกิน | `ResolveSkins`: หา `<body|head|face>_..._1` ของ skeleton นั้น แล้วรวมเป็น `Skin` เดียว — body_1110901 ได้ `body_1110901_1 + head_1110901_1 + face_1110901_1`; อีก 4 id มีแต่สกิน `default` จึงใช้ default ตามเดิม (มี warning) |
| ท่า | setup pose (`SetToSetupPose` + `UpdateWorldTransform`) |
| canvas | สูง 1024 canvas px, padding 16, กว้างตาม bounds ของ skeleton, scale เดียวทุกชิ้นต่อ id |
| render scale | **2x** ทุก pack (ภาพจริง = 2 เท่าของ canvas px); ทุกไฟล์ต่ำกว่า 25 MB จึงไม่ต้องลด scale |
| กล้อง | orthographic, clear = โปร่งใส, 1 world unit = 1 canvas px |
| ภาพ | premultiplied จาก RT → straight alpha ก่อนเขียน PNG |
| shader | override เป็น `Spine/Skeleton` บน material ชั่วคราว (atlas ใช้ `Spine-SkeletonGraphic` ซึ่งมี `ZTest [unity_GUIZTestMode]`) — ไม่แตะ asset |
| ซ่อน slot | `slot.Attachment = null` บนอินสแตนซ์ skeleton แล้ว `LateUpdate()` ต่อ slot (asset ไม่ถูกแก้) |
| crop | ขอบ alpha (`a > 0`); `x = (2·left + w) / (2·scale)`, y นับจากขอบบนลง (y ชี้ลง) |
| ฉาก | ใช้ฉากเดิมชั่วคราว (batchmode มี untitled scene ที่สร้าง additive ไม่ได้) แล้ว destroy ทุก object ที่สร้าง |

## 3. ผลลัพธ์ทั้ง 5 pack

canvas / จำนวนชิ้น / กลุ่ม / ขนาดไฟล์ / mismatch (tol 2/255, เทียบ full ref)

| id | canvas | parts | groups | JSON | mismatch_fraction |
|---|---:|---:|---:|---:|---:|
| body_1110901 | 798×1024 | 18 | 11 | 3.84 MB | **0.000307** (1,002 px) |
| 410041_1 | 1458×1024 | 14 | 7 | 4.24 MB | **0.000071** (424 px) |
| 460011_1 | 1192×1024 | 10 | 6 | 5.02 MB | **0.000014** (67 px) |
| 1110011 | 2942×1024 | 9 | 3 | 13.15 MB | **0.000044** (531 px) |
| 570036 | 962×1024 | 1 | 1 | 2.62 MB | **0.000000** (0 px) |

`verify_pack.py` ตรวจเพิ่มและผ่านทั้ง 5: ขนาดภาพ = `w`×2 / `h`×2 ทุกชิ้น, `(x−w/2)·2` และ `(y−h/2)·2`
เป็นพิกเซลเต็ม, crop box อยู่ใน canvas, alpha-over ตาม `order` แล้ว painted px เท่ากับ full ref

## 4. กลุ่มของ body_1110901 (11 กลุ่ม, 18 ชิ้น — ลำดับ = ลำดับหน้าเว็บ)

| # | group | label | parts | slots |
|---:|---|---|---:|---|
| 1 | hair_front | Hair (front) | 2 | `hair_f1`, `hair_f2` |
| 2 | hair_back | Hair (back) | 1 | `hair_b` |
| 3 | head | Head | 1 | `head` |
| 4 | face_open | Face (open) | 1 | `face` |
| 5 | torso | Torso | 1 | `body` |
| 6 | lower_body | Lower body | 4 | `zuohoubai`, `youhoubai`, `youbai`, `zuobai` |
| 7 | arm_L | Left arm | 2 | `zuoshou`, `zuojian` |
| 8 | arm_R | Right arm | 2 | `youshou`, `youjian` |
| 9 | leg_L | Left leg | 1 | `zuojiao` |
| 10 | leg_R | Right leg | 1 | `youjiao` |
| 11 | sash_or_ribbon | Sash / ribbon | 2 | `piaodai2`, `piaodai1` |

- ไม่มี slot ตกไป `other`; กลุ่มที่ไม่มีชิ้นถูกตัดออกจาก `groups[]`: `face_alt`, `accessory`, `other`
- ข้าม: `w_dao/w_zhang/w_qiang/w_jian` (ไม่มี attachment), `root_boundingBox` (BoundingBox ไม่วาด)

## 5. Preview page (`extracted/web/index.html`)

- ไฟล์เดียว ไม่มี dependency; รายชื่อ pack มาจาก `packs.json` เท่านั้น (หน้าเว็บไม่มี id list ในโค้ด)
  - tool เขียน `packs.json` จาก `*.pack.json` ที่มีจริงในโฟลเดอร์: export ตัวใหม่ → โผล่เอง,
    ลบไฟล์ → หลุดจากสารบัญเอง; `defaultPack` = ตัวที่บันทึกไว้ ถ้ายังมีอยู่ ไม่งั้น `body_1110901`
  - เปิด `?pack=<id>` เพื่อเลือก pack ตรง ๆ (ใช้ได้แม้ id ยังไม่อยู่ในสารบัญ)
  - `file://` ใช้ไม่ได้เพราะ fetch ถูกบล็อก → มี file picker สำรอง
- แสดง: canvas ต่อ scale พอดี, เลเยอร์ซ้อนตาม `order`, checkbox ต่อกลุ่ม, คลิกชื่อกลุ่ม = isolate,
  hover ชิ้นส่วน = โชว์ `slot:attachment` + x/y/w/h
- ตรวจด้วย headless Chrome (`--dump-dom`): 18 `<img>` + 11 `<li>` สำหรับ body_1110901,
  14 + 7 สำหรับ `?pack=410041_1`, 9 + 3 สำหรับ `?pack=1110011` (Smoke mass 2 / Smoke wisp 1 / Embers 6),
  ไม่มี error ใน console, label มาจาก `groups[].label` ของ map จริง

## 6. observed / inferred / unknown

### observed
- 5 pack, reassembly ต่างสูงสุด 0.031 % (body_1110901), ตัวอื่น ≤ 0.007 %; ทุกไฟล์ < 25 MB
- `410041_1` = กวาง/กิเลนขาวมีผ้าแดง, `460011_1` = ภูตเห็ดหมวกแดง, `1110011` = เอฟเฟกต์ควัน/หมึก,
  `570036` = ต้นไม้บอนไซ (ดูจาก `_<id>.pack_full_ref.png`)
- `410041_1` ชิ้นใหญ่สุดคือ `piaodai3` (54 % ของ canvas) และ `piaodai1` (42 %) — ผ้าขาวที่กินพื้นที่ silhouette
- `460011_1` ชิ้น `tou` เดี่ยว = 56 % ของ canvas (หัว + หมวกเห็ดทั้งใบ)
- `1110011` canvas กว้าง 2942 px เพราะเอฟเฟกต์แผ่แนวนอน (สัดส่วน 2.87:1)

### inferred
- การแบ่งกลุ่มของ `410041_1`/`460011_1` มาจากชื่อ slot ของ binary — เป็นการตีความชื่อจีน ไม่ใช่ข้อมูลที่ asset ประกาศ
- การแบ่ง 3 กลุ่มของ `1110011` เป็นการอ่านจากภาพ (ควันก้อนหนา / ริ้วบาง / ลิ้นไฟ) ไม่มีข้อมูลใน asset บอก;
  `570036` ยังเป็นกลุ่มเดียวเพราะมีชิ้นเดียว
- `460011_1` `xiaomogu` (เห็ดเล็ก) แยกกลุ่มจาก `ye*` (ใบ) ตามชื่อ — ตำแหน่งในภาพอยู่ข้างหมวกทางขวา

### unknown
- หน้าเว็บ "Anatomy of a disciple" ไม่ได้อยู่ใน repo นี้ → ยังไม่ได้ทดสอบโหลดจริงนอกจากหน้า preview ของเราเอง
- ไม่รู้ว่า `1110011`/`570036` ถูกใช้เป็น "body" ของ disciple จริงหรือไม่ (ชื่อ slot/ภาพไม่เหมือนตัวละคร)
- ยังไม่ได้ทดสอบว่า pack เหล่านี้ควรใช้ canvas สูงเท่ากัน (1024) หรือปรับตามขนาดตัวละครจริงบนหน้าเว็บ
