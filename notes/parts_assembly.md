# Parts assembly — body_1110901

ตัดชิ้นส่วนจาก atlas เป็น Sprite/Mesh แยกชิ้น แล้วประกอบกลับในฉากตาม setup pose
พร้อมสลับมุมมอง Assembled / Exploded

- วันที่รัน: 2026-10-24
- Unity: 2021.3.45f2 (โปรเจกต์ `C:\Workspace\REA\my_clone`)
- ต้นทาง (อ่านอย่างเดียว ไม่แก้): `Assets/_LocalReference/Spine/body_1110901/`
  (`body_1110901.atlas.txt`, `.png`, `.skel.bytes`, `SkeletonData.asset`)
- ไฟล์ที่สร้างทั้งหมด: `Assets/_LocalReference/Parts/body_1110901/` + ฉาก `Assets/_LocalReference/PartsAssembled.unity`
  (ทั้ง `Assets/_LocalReference/` ถูก ignore ที่ `.gitignore` บรรทัด 175 → `git check-ignore` ยืนยันทั้ง sprite, mesh, index และฉาก)
- ไม่คัดลอก DLL/โค้ดจากเกม และไม่ได้แก้ไข asset ต้นทางใน `Spine/`

## 1. Atlas (observed)

`body_1110901.atlas.txt` — หน้าเดียว `body_1110901.png` ขนาด **1152×80**

| รายการ | ค่า |
|---|---|
| จำนวน region | **52** |
| `rotate: true` | **18** |
| `rotate: 90` | **0** (ไม่มี) |
| `rotate: false` | 34 |
| `offset` ไม่ใช่ `0,0` | **0** (ทุก region เป็น `0, 0`) |
| `orig` ≠ `size` | **2** — `1110901_1tou` (orig 60×76 / size 55×76), `1110901_3tou` (orig 61×76 / size 60×76) |
| `index` | ทุก region = `-1` |
| `pma: true` ในไฟล์ | **ไม่มี** (ไม่มีบรรทัด `pma` เลย) |
| ภาพจริงเป็น premultiplied | **ใช่** — 37,353 px ที่ alpha > 0 ไม่มี px ใดที่ RGB > A+1, และ 0 px ที่ A=0 แต่ RGB > 0 (ภาพ premultiplied ต้องเป็นเช่นนี้) |

ชื่อ region ทั้ง 52 ตัวไม่มี `/` จึงไม่มีไฟล์ใดถูกเปลี่ยนชื่อเป็น `__`
(โค้ดรองรับและเก็บแมป `region` → `file` ไว้ใน `parts_index.json` อยู่แล้ว)

รายชื่อ region ทั้งหมด (52): `1110901_{1,2,3}` ×
`bin, houfa, jian, liuhai, piaodai1, piaodai2, shenti, tou, wuguan*, youbai, youhoubai, youjian, youjiao, youshou, zuobai, zuohoubai, zuojiao, zuoshou`
(*`wuguan` มีเฉพาะชุด `1`; `2tou` และ `3tou` มีขนาดต่างกันตามตารางข้างบน)

## 2. Skeleton (observed)

อ่านด้วย spine-csharp 3.8 ที่มีในโปรเจกต์ (`SkeletonBinary` + `Atlas`) นอก Unity
(ระหว่างตรวจ Unity editor เปิดโปรเจกต์อยู่ จึงรัน batchmode ไม่ได้)

- slot **23**, bone **64**, skin **8** (มี `default`), animation **23**
- attachment ทั้งไฟล์: Region **1**, Mesh **17**, LinkedMesh **34**, BoundingBox **1**

| skin | attachments | slots | Region | Mesh | LinkedMesh | อื่น ๆ |
|---|---:|---:|---:|---:|---:|---:|
| default | 1 | 1 | 0 | 0 | 0 | 1 (BoundingBox) |
| body_1110901_1 | 13 | 13 | 0 | **13** | 0 | 0 |
| body_1110901_2 | 13 | 13 | 0 | 0 | 13 | 0 |
| body_1110901_3 | 13 | 13 | 0 | 0 | 13 | 0 |
| face_1110901_1 | 1 | 1 | **1** | 0 | 0 | 0 |
| head_1110901_1 | 4 | 4 | 0 | **4** | 0 | 0 |
| head_1110901_2 | 4 | 4 | 0 | 0 | 4 | 0 |
| head_1110901_3 | 4 | 4 | 0 | 0 | 4 | 0 |

ชุดที่ประกอบ (เหมือน `SpineUiActor`): `body_1110901_1` + `head_1110901_1` + `face_1110901_1`
= **18 attachment: Mesh 17 + Region 1 (94.4 % เป็น Mesh)**

> **จุดหยุดตามกติกา:** Mesh เกินครึ่ง → รายงานก่อนลงมือ และผู้ใช้เลือก
> **"คงการประกอบแบบ mesh"** (17 mesh จริง + face เป็น sprite) จึงเดินหน้าตามนั้น
> ทางเลือกที่ไม่ได้เลือก: ตัดเฉพาะ region แล้วข้าม mesh, หรือแทน mesh ด้วย quad sprite

## 3. Tools

| Tool | ไฟล์ | เมนู |
|---|---|---|
| Slice Atlas Parts | `Parts/body_1110901/Editor/SliceAtlasParts.cs` | `Tools/LocalReference/Slice Atlas Parts` |
| Assemble Parts In Scene | `Parts/body_1110901/Editor/AssemblePartsInScene.cs` | `Tools/LocalReference/Assemble Parts In Scene` |
| Layout switch (runtime) | `Parts/body_1110901/AssembledPartsLayout.cs` | component บน root ของฉาก |

### Slice Atlas Parts

- อ่าน `.atlas.txt` (parser ในไฟล์เดียวกัน) → ตัดทีละ region จาก PNG หน้า atlas
- `rotate: true` หมุนกลับด้วยการแปลงพิกเซลจริง (`out[py,px] = page[x+py, y+w-1-px]`) ไม่ใช่หมุน GameObject
- วางพิกเซลกลับใน canvas ขนาด `orig` ที่ตำแหน่ง `(offsetX, origH - offsetY - h)` → pivot กลางตรงกับ RegionAttachment
- ตัวเลือกในหน้าต่าง: `Pixels Per Unit` (เริ่ม 100) และ `Un-premultiply Alpha` (เปิดไว้)
  — atlas ไม่ได้ประกาศ `pma` แต่ภาพเป็น premultiplied จริง จึง un-premultiply ก่อนเซฟเพื่อกันขอบดำ
- ตั้ง importer: Sprite (Single), PPU 100, Compression None, mipmap ปิด, Alpha Is Transparency ตามชนิด alpha ของสไปรต์
  (52/52 ไฟล์ได้ค่าเดียวกัน: `textureType 8`, `spriteMode 1`, `spritePixelsToUnits 100`, `alphaIsTransparency 1`, compression 0)
- เขียน `parts_index.json` (atlas, page, options, 52 parts พร้อม rotate/xy/size/orig/offset/index)
- **ตรวจ import setting ของหน้า atlas อัตโนมัติทุกครั้งที่กด Slice** (มีปุ่ม `Check + Fix Atlas Import Settings`
  และเมนู `Tools/LocalReference/Check Atlas Import Settings` แยกด้วย)
  - อ่านพิกเซลหน้า atlas แล้วบอกชนิด alpha ได้เอง: พิกเซล premultiplied ไม่มีทางมี channel สว่างกว่า alpha
    → เจอตัวแรกที่สว่างกว่า (เกิน 2/255) = straight, ไม่เจอเลยและมีพิกเซลโปร่งใส = premultiplied
  - เทียบกับ `pma:` ที่ atlas ประกาศ (ถ้ามี) แล้วรายงานเมื่อไม่ตรง — **ใช้พิกเซลตัดสิน**
  - ตั้ง `Alpha Is Transparency` ให้ตรงชนิด (`false` สำหรับ PMA, `true` สำหรับ straight) พร้อม `mipmap off`,
    `Compression None`, `Alpha Source From Input`, `Sprite Mode None`, `Max Size ≥ หน้า atlas`
  - ที่มาของกฎ: `MaterialChecks.IsTextureSetupProblematic` (PMA + Alpha Is Transparency → ขอบขาว)
    และ `AssetUtility.SetDefaultTextureSettings` ใน spine-unity — ไม่ได้คิดกฎเอง
  - สไปรต์ที่ตัดออกมาตั้ง `Alpha Is Transparency = unpremultiply || page straight`
    (สไปรต์จากหน้า PMA ที่ไม่ได้ un-premultiply ต้องปิด)
- **ผลตรวจ (observed):** ตั้งใจทำ importer พัง (`alphaIsTransparency 1`, mipmap เปิด) → check จับได้ 2 ข้อ,
  fix แล้วได้ `alphaIsTransparency False`, mipmap ปิด, และ `spriteMode Multiple → None`;
  ตัดซ้ำทั้ง 52 region + `parts_index.json` ได้ **sha256 เท่าเดิมทุกไฟล์** (ไม่มี drift จากโค้ดใหม่)

**ผลตรวจการตัด (observed):** สร้างภาพคาดหวังจากหน้า atlas ใหม่ทั้ง 52 region (un-rotate + padding + un-premultiply)
แล้วเทียบกับ PNG ที่เซฟ → **max |diff| = 0 ทุกช่องสี ทุกไฟล์**, ขนาดตรง `orig` ทั้ง 52, ครอบทั้ง 18 region ที่ rotate

### Assemble Parts In Scene

- เลือก `SkeletonDataAsset` + skin 3 ช่อง (Body/Head/Face) จาก dropdown ที่กรองด้วย prefix `body_` / `head_` / `face_`
- รวม skin เป็นชุดเดียว (`Skin.AddSkin`), `SetSkin`, `SetToSetupPose()`, `UpdateWorldTransform()`
- ไล่ตาม `skeleton.DrawOrder`:
  - **RegionAttachment** → GameObject + SpriteRenderer ใช้ sprite ที่ตัดไว้; ตำแหน่ง/หมุน/สเกลคำนวณจาก bone world matrix รวม `x, y, rotation, scaleX, scaleY` ของ attachment; สี = slot color × attachment color
  - **MeshAttachment / LinkedMesh** → Mesh จาก world vertices ใน setup pose + UV + triangles ของ attachment, บันทึกเป็น `.asset`, ใส่ MeshFilter/MeshRenderer ด้วย material ที่ใช้ texture หน้า atlas (ไม่แปลงเป็น Sprite)
  - ชนิดอื่น (BoundingBox) → ข้ามและบันทึกชื่อ
- `sortingOrder` = ลำดับ draw order; ชื่อ GameObject = `<slot>:<attachment>`
- บันทึกฉากทับ `Assets/_LocalReference/PartsAssembled.unity` และสร้างกล้อง `AssembledCamera` (orthographic, fit จาก bounds ของทุก renderer)
- บน root ใส่ `AssembledPartsLayout` เริ่มที่โหมด Assembled
- สร้างในฉากใหม่แบบ **additive** แล้วเปิดผลลัพธ์ค้างไว้: ฉากอื่นที่เปิดอยู่ไม่ถูกแตะ
  ถ้า `PartsAssembled.unity` เปิดอยู่และยังไม่บันทึก จะยกเลิกพร้อมข้อความ (ไม่ทับงานที่ยังไม่เซฟ); ถ้าเปิดอยู่แต่สะอาดจะปิดก่อนเขียนทับ
- รองรับ skin ใดก็ได้ใน dropdown รวมชุดที่เป็น **LinkedMesh** (ทดสอบ body/head 1, 2, 3 แล้ว — ดู §4.1)

**โหมดสลับ (ตรวจจริงผ่าน editor):** `Assembled` และ `Exploded`
→ Exploded ได้ 18 ชิ้นในกริด 5 คอลัมน์ เรียงตามชื่อ, ป้ายชื่อ 18 อัน, สลับกลับได้ตำแหน่งเดิม **error 0.0** และป้ายถูกลบหมด

## 4. ฉากที่ได้

- root: `Assembled_body_1110901` (ชิ้นส่วนทั้ง 18 อยู่ใต้ root นี้) + `AssembledCamera` เป็น root แยกอีกอัน
- ชิ้นส่วน **18** ชิ้น: MeshRenderer **17** + SpriteRenderer **1** (face)
- mesh assets: 17 ไฟล์ใน `Parts/body_1110901/Meshes/` (ชื่อตาม region)
- material: `Parts/body_1110901/AssembledPartsMaterial.mat` (shader `Spine/Skeleton`, texture = หน้า atlas)
- กล้อง: ortho size 0.9757583, ตำแหน่ง (0.2526, 0.7504, −10) — เห็นตัวละครเต็มเฟรม 1920×1080

### 4.1 ประกอบซ้ำได้ทุก skin (รวม LinkedMesh) — observed

กด Assemble ใหม่ด้วย skin คู่ใดก็ได้; ทดสอบครบทั้ง 3 ชุด (face มีชุดเดียวคือ `face_1110901_1`):

| body + head | ชิ้น | Mesh | ในนั้นเป็น LinkedMesh | vertex error vs runtime | triangles ผิด | sorting ผิด | UV error | UV อยู่นอก region ของตัวเอง |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `_1` + `_1` | 18 | 17 | 0 | 0.0 | 0 | 0 | 0.0 | 0/17 |
| `_2` + `_2` | 18 | 17 | **17** | 0.0 | 0 | 0 | 0.0 | 0/17 |
| `_3` + `_3` | 18 | 17 | **17** | 0.0 | 0 | 0 | 0.0 | 0/17 |

- LinkedMesh ใช้ geometry/triangles จาก parent mesh แต่ UV ถูก map เข้า region ของ variant ตัวเอง
  (`1110901_2*` / `1110901_3*`) — ตรวจ UV bounds เทียบ rect ใน `parts_index.json` แล้วอยู่ข้างในครบ 17/17
- หลังทดสอบ ได้ประกอบ `_1`+`_1` กลับเป็นชุดส่งมอบ: mesh ทั้ง 17 ไฟล์เทียบกับ backup ก่อนทดสอบได้
  **pos/color/UV ต่าง 0, triangles เท่ากัน** และเรนเดอร์ออกมา **เหมือนเดิมทุกพิกเซล** (max diff 0, 0 px ต่าง)
- ภาพตัวอย่างต่อ variant: `_verify/assembled_body_1110901_1.png`, `_2.png`, `_3.png`
- ข้อจำกัด: `Meshes/` เก็บได้ทีละชุด — ประกอบ variant ใหม่จะลบ mesh ชุดเก่าและเขียนทับ
  (ไม่มีปนกัน) ฉากจะแสดงชุดที่ประกอบล่าสุดเสมอ

## 5. ตรวจเทียบกับ SpineUiActor (observed)

วิธี: เปิด `PartsAssembled.unity` และ `_verify/ReferenceSpineUiActor.unity` แบบ **additive** (ไม่แตะฉากที่ผู้ใช้เปิดอยู่)
เรนเดอร์ด้วย `Camera.Render` ลง RenderTexture 1920×1080 พื้นหลังโปร่งใส; ฉากอ้างอิงใช้ `SpineUiActor`
ที่ตั้ง `skinParts` = body_1110901_1 / head_1110901_1 / face_1110901_1 และถูกย่อ/เลื่อน (4 รอบ) ให้ content bbox ตรงกับฝั่ง assembled

กล้องเรนเดอร์วัตถุจาก **ทุกฉากที่โหลดอยู่** จึงเลื่อน root ของ assembled + กล้องไป (500, 500) พร้อมกัน
และวางกล้องของฉากอ้างอิงไว้ที่ (500, 500) เพื่อไม่ให้วัตถุจากฉากอื่นหลุดเข้ามาในภาพเทียบ

| การวัด | ค่า |
|---|---|
| content bbox (px) | **เหมือนกันเป๊ะ** ทั้งสองภาพ: x 635, y 118, 650×844 |
| พิกเซลที่มี alpha > 8 | 232,061 (assembled) / 232,106 (SpineUiActor) |
| alpha mask ต่างกัน | 1,101 px (0.47 %) |
| RGB mean abs diff (เฉพาะที่ทึบทั้งคู่) | **1.39 / 255** (p99 = 7, max 74) |
| พิกเซลต่าง > 32 | **28 px** (จาก 193,948 px ที่ทึบทั้งคู่) |
| พิกเซลต่าง > 64 | 9 px |

### ตำแหน่ง / ลำดับซ้อน (ตรวจเชิงเลข ไม่ได้ดูจากภาพ)

เทียบ mesh ในฉากกับ `ComputeWorldVertices` ของ runtime ทีละชิ้น (setup pose, สเกล 0.01):

| การวัด | ผล |
|---|---|
| ระยะห่างจุดยอดสูงสุด (17 mesh) | **2.35 × 10⁻⁷ หน่วย** (≈ 0.0001 px) |
| triangles ตรงกับ runtime | 17/17 |
| UV ตรง (หลังแปลง V ตาม Unity) | max 6.9 × 10⁻⁸ |
| face quad เทียบ RegionAttachment | 3.4 × 10⁻⁴ หน่วย |
| `sortingOrder` = draw order | 18/18 (0,1,6–17,19–22) |
| slot color / attachment color | ขาวทั้งหมด (1,1,1,1) |

→ **ไม่มีความต่างด้านตำแหน่งหรือลำดับซ้อน** ในทุกชิ้น

### สี / ขอบ — ชิ้นที่ต่าง

attributable ต่อชิ้น (พิกเซลที่ชิ้นนั้นอยู่บนสุด):

| draw | ชิ้น | ชนิด | พิกเซลที่เห็น | mean diff | > 8 | > 32 |
|---:|---|---|---:|---:|---:|---:|
| 0 | hair_b:1110901_1houfa | Mesh | 9,160 | 0.35 | 0 | 0 |
| 1 | youshou:1110901_1youshou | Mesh | 2,896 | 1.70 | 5 | 0 |
| 6 | piaodai2:1110901_1piaodai2 | Mesh | 6,116 | 1.37 | 37 | **19** |
| 7 | zuohoubai:1110901_1zuohoubai | Mesh | 10,249 | 1.96 | 75 | 0 |
| 8 | youhoubai:1110901_1youhoubai | Mesh | 1,472 | 2.86 | 3 | 0 |
| 9 | youjiao:1110901_1youjiao | Mesh | 2,866 | 1.11 | 0 | 0 |
| 10 | youbai:1110901_1youbai | Mesh | 3,073 | 1.18 | 2 | 0 |
| 11 | zuojiao:1110901_1zuojiao | Mesh | 7,131 | 1.22 | 57 | 0 |
| 12 | zuobai:1110901_1zuobai | Mesh | 8,110 | 1.16 | 20 | 0 |
| 13 | body:1110901_1shenti | Mesh | 40,184 | 1.81 | 91 | 0 |
| 14 | youjian:1110901_1youjian | Mesh | 1,320 | 1.92 | 0 | 0 |
| 15 | piaodai1:1110901_1piaodai1 | Mesh | 14,464 | 2.19 | 180 | 0 |
| 16 | zuoshou:1110901_1zuoshou | Mesh | 7,064 | 1.92 | 23 | 0 |
| 17 | zuojian:1110901_1jian | Mesh | 3,957 | 1.67 | 3 | 0 |
| 19 | head:1110901_1tou | Mesh | 39,539 | 0.79 | 12 | 0 |
| 20 | hair_f1:1110901_1bin | Mesh | 3,450 | 1.33 | 2 | 0 |
| 21 | face:1110901_1wuguan | Sprite | 21,495 | **1.41** | 219 | **9** |
| 22 | hair_f2:1110901_1liuhai | Mesh | 11,402 | 0.83 | 0 | 0 |

**หลังแก้ import (ดู §5.1) ไม่มีชิ้นที่ต่างชัดแล้ว** — เหลือ 28 px ที่ต่าง > 32 จาก 193,948 px ที่ทึบทั้งคู่
กระจายที่ `piaodai2` 19 px กับ `face` 9 px; ชิ้นอื่น 0 px
mean diff 0.35–2.86/255 เป็นความต่างแนวขอบ/พิกเซลกึ่งโปร่งใส (mesh PMA กับ sprite straight alpha) ไม่ใช่ตำแหน่ง

### 5.1 แก้กรอบขาวที่ face (ทำแล้ว)

- **สาเหตุ (observed):** `Spine/body_1110901/body_1110901.png.meta` ตั้ง `alphaIsTransparency: 1`
  ทำให้ Unity เติม RGB ลงพิกเซลที่ alpha = 0 (alpha dilation) — อ่าน texture ที่ import แล้วในช่วง footprint ของ face (22×32):
  **111 px ที่ alpha = 0 มี 84 px ที่ RGB > 8 (สูงสุด 181)** ทั้งที่ไฟล์ PNG ต้นฉบับมี RGB = 0 ทุกพิกเซลที่ alpha = 0
  จากนั้น material ของ Spine (`_StraightAlphaInput 0` = PMA blending) บวก RGB นั้นเข้าไปแม้ alpha = 0 → เป็นกรอบขาวทับผิว
- **วิธีแก้:** ตั้ง `alphaIsTransparency = false` ที่ texture หน้า atlas แล้ว reimport
  (ตรงกับที่ spine-unity ตั้งเองใน `AssetUtility.SetDefaultTextureSettings`: "Prevent the texture importer from applying bleed to the transparent parts for PMA.")
- **ตรวจหลังแก้:** 111 px ที่ alpha = 0 ใน footprint ของ face เหลือ **0 px ที่ RGB > 8 (สูงสุด 0)**
- **ผลต่อภาพเทียบ:** > 32 จาก **1,921 → 28 px**, mean 1.94 → **1.39**, p99 32 → **7**, max 149 → **74**;
  face mean 6.63 → **1.41**, face > 32 1,890 → **9**; กรอบขาวหายจากภาพ SpineUiActor
- ไฟล์ที่แก้ใต้ `Spine/`: เฉพาะ `.meta` (import setting) — ไม่ได้แตะ PNG/atlas/skel
- ข้อสังเกต: กรอบขาวเดียวกันเคยปรากฏใน `extracted/ref_sheets/body_1110901/full_alpha.png` (SkeletonAnimation)
  จึงเป็นของ texture/material path ไม่ใช่ของ UI renderer; ไฟล์ ref sheet ชุดเดิมยังไม่ได้ export ใหม่หลังแก้

## 6. ชิ้นที่ข้าม / ไม่ได้วาด

- ไม่มี attachment ชนิด Region/Mesh ที่สร้างไม่ได้ — สร้างครบ 18/18
- ชิ้นที่ข้ามเพราะไม่ใช่ Region/Mesh: `root_boundingBox:root_boundingBox` (BoundingBoxAttachment — ไม่มีพิกเซล)
- slot ที่ว่างในชุด skin นี้ (ไม่มี attachment): `w_dao`, `w_zhang`, `w_qiang`, `w_jian` (4 ช่อง)
- ชุด skin ที่ประกอบครอบ 18/23 slot; `default` skin (ถ้าใส่ด้วย) จะเพิ่มเฉพาะ bounding box

## 7. ข้อจำกัด

- **ไม่มีการ deform ตามกระดูก**: mesh ถูกอบจุดยอด ณ setup pose → ขยับ/หมุน bone ทีหลังไม่ได้รูปตาม
  (ไม่ใช่ SkinnedMeshRenderer) และไม่มี animation เลย — ฉากนี้เป็นภาพนิ่งของ setup pose เท่านั้น
- **สองเส้นทาง alpha อยู่ในฉากเดียวกัน**: mesh 17 ชิ้นใช้ texture หน้า atlas (premultiplied) ผ่าน material `Spine/Skeleton`,
  ส่วน face ใช้ PNG ที่ un-premultiply แล้วผ่าน SpriteRenderer — ขอบจึงอาจต่างจาก Spine ได้เล็กน้อย
- ไม่รองรับ blend mode / clipping / tint black / separator slots ของ Spine
- สลับ skin: กด Assemble ใหม่ได้ทุกชุด (ทดสอบ 1/2/3 แล้ว) แต่ `Meshes/` เก็บได้ทีละชุด
  — การประกอบใหม่จะลบและสร้าง mesh ทับ จึงเก็บสอง variant พร้อมกันไม่ได้
- การแก้กรอบขาวทำที่ import setting ของ texture หน้า atlas (`alphaIsTransparency=false`) ซึ่งอยู่ใน `Spine/`
  (เฉพาะ `.meta`; ไม่ได้แก้ pixel/atlas/skel)
- Exploded mode: กล้องไม่ได้ refit — ชิ้นที่กระจายออกไปอาจหลุดเฟรมเมื่อสลับ (ยังไม่ได้วัด)
- ป้ายชื่อ Exploded ใช้ TextMesh (legacy font) และถูกสร้างเฉพาะตอนอยู่โหมด Exploded
- ฉากมี 2 root (`Assembled_body_1110901` + `AssembledCamera`) ไม่ใช่ root เดียวสำหรับทั้งฉาก
- ภาพเทียบเป็น `Camera.Render` ลง RenderTexture ไม่ใช่การกด screenshot ของ Game view โดยตรง (พิกเซลเทียบเท่า แต่ไม่มี UI overlay)

## 8. observed / inferred / unknown

### observed

- atlas: 52 region, 18 rotate, offset เป๊ะ 0 ทุกตัว, orig ต่างจาก size 2 ตัว, ไม่มี `pma` แต่ภาพ premultiplied
  (0 px ที่ RGB > A+1; 0 px ที่ A=0 แต่ RGB > 0)
- ตัด sprite แล้วเทียบกลับได้ **diff 0** ทั้ง 52 ไฟล์; importer ตั้งครบ 52 ไฟล์เหมือนกัน
- skeleton: 23 slot / 64 bone / 8 skin / 23 animation; ชุดที่ใช้ = 17 Mesh + 1 Region
- ตำแหน่ง mesh ในฉากตรง runtime 2.35e-7 หน่วย, triangles ตรง, UV ตรง, sortingOrder = draw order 18/18
- ภาพ assembled กับ SpineUiActor มี bbox เดียวกันเป๊ะ; หลังแก้ import ต่างกัน **28 px > 32** (จาก 193,948 px ที่ทึบทั้งคู่)
- texture ที่ import แล้ว: footprint ของ face มี 111 px ที่ alpha = 0; ก่อนแก้ **84 px มี RGB > 8 (max 181)**, หลังตั้ง `alphaIsTransparency=false` เหลือ **0 px (max 0)**
- face ใน SpineUiActor ไม่มีกรอบขาวแล้ว; ที่เหลือ > 32 คือ `piaodai2` 19 px กับ `face` 9 px
- ประกอบซ้ำด้วย body/head 2 และ 3 ได้ 18 ชิ้น (17 LinkedMesh), vertex/triangles/sorting/UV error 0, UV อยู่ใต้ region ของ variant ตัวเอง 17/17
- หลังทดสอบ ประกอบ `_1`+`_1` กลับได้ mesh เหมือน backup (pos/color/UV ต่าง 0, triangles เท่ากัน) และเรนเดอร์เหมือนเดิมทุกพิกเซล
- สลับ Assembled ↔ Exploded ได้: 18 ชิ้น, 5 คอลัมน์, 18 ป้าย, กลับมา error 0
- ทุกไฟล์อยู่ใต้ `Assets/_LocalReference/` ซึ่งถูก ignore (`.gitignore` บรรทัด 175)

### inferred

- ความต่างที่เหลือ (mean 0.35–2.86/255, 28 px > 32) เกิดจาก alpha blending/filter ต่างเส้นทาง (PMA mesh vs sprite straight alpha)
  และการ resample ตอน fit กล้อง — ไม่ใช่ตำแหน่ง เพราะจุดยอด/UV ตรง runtime แล้ว
- `1110901_1tou` และ `1110901_3tou` มี size แคบกว่า orig แสดงว่าตอน pack มีการตัดคอลัมน์โปร่งใสออก (offset ยังเป็น 0)

### unknown

- ตัวเกมจริงที่รันอยู่ใช้ import setting/material ชุดเดียวกับโปรเจกต์นี้หรือไม่ — ยังไม่ได้เทียบ
  (ฉากอ้างอิงในโปรเจกต์ไม่มีกรอบขาวแล้ว หลังตั้ง `alphaIsTransparency=false`)
- atlas อื่นในโปรเจกต์อาจตั้ง `alphaIsTransparency: 1` เหมือนกัน — ยังไม่ได้ตรวจทั้งโปรเจกต์
- atlas หลายหน้า, ชื่อ region ที่มี `/` — โค้ดรองรับแต่ยังไม่ทดสอบ
- การเทียบในกล้อง/ความละเอียดอื่น (ภาพนี้ fit ให้ bbox ตรงกันแล้ว)
- ผลของ Standalone platform override ที่ตั้ง compression 1 (overridden 0) ต่อ build จริง — ยังไม่ได้ build ทดสอบ
- `face` มี skin เดียว จึงทดสอบได้แค่ `face_1110901_1`

## 9. ไฟล์ที่เกี่ยวข้อง

```
Assets/_LocalReference/
  PartsAssembled.unity                     ฉาก assembled/exploded
  Parts/body_1110901/
    Editor/SliceAtlasParts.cs              tool ตัด atlas
    Editor/AssemblePartsInScene.cs         tool ประกอบฉาก
    AssembledPartsLayout.cs                สลับ Assembled/Exploded + ป้ายชื่อ
    AssembledPartsMaterial.mat             material ของ mesh (Spine/Skeleton)
    Sprites/*.png (52) + .meta             ชิ้นส่วนที่ตัดแล้ว (PPU 100)
    Meshes/*.asset (17)                    mesh ของ attachment
    parts_index.json                       แมป region → ไฟล์ + ค่าจาก atlas
    _verify/
      attachments_by_skin.json             จำนวน attachment แยกชนิดต่อ skin
      runtime_setup_pose.json              จุดยอด world/setup pose จาก runtime
      scene_vs_runtime.json                ผลเทียบ mesh ในฉากกับ runtime
      slice_verification.json              ผลเทียบ sprite กับ atlas
      assembled_gameview.png               เรนเดอร์ assembled 1920×1080 (หลังแก้ import)
      reference_spineuiactor_gameview.png  เรนเดอร์ SpineUiActor 1920×1080 (framing เดียวกัน)
      assembled_body_1110901_1.png         ภาพตัวอย่าง variant 1
      assembled_body_1110901_2.png         ภาพตัวอย่าง variant 2 (LinkedMesh)
      assembled_body_1110901_3.png         ภาพตัวอย่าง variant 3 (LinkedMesh)
      render_diff_attributed.json          diff ต่อชิ้น (พิกเซลบนสุด) — ใช้ค่าในรายงาน
      ReferenceSpineUiActor.unity          ฉากอ้างอิง SpineUiActor (skin ชุดเดียวกัน)
```
