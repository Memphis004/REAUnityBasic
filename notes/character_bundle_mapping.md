# Character id → Spine part bundles (dbbodyconfig / dbcomponentcfg)

วันที่: 2026-10-23
แหล่งข้อมูล (อ่านอย่างเดียว): `game_copy/Immortal Clan Grandmastery_Data`, `extracted/ilspy/Assembly-CSharp`,
`extracted/dbbodyconfig.json`, `extracted/dbcomponentcfg.json`

---

## 0. สรุปสั้น

เกมไม่ได้ map "character id" ตรง ๆ ไปยังไฟล์ bundle แต่มี **2 ตาราง**:

| ตาราง | ใช้ทำอะไร | จำนวนแถว |
|---|---|---:|
| `dbbodyconfig` | id ของ **body/หุ่น** → bundle หลัก + skin ของตัว + ขนาด/เงา/ตำแหน่งหัว | 4,574 (มี `spineAb` 4,463) |
| `dbcomponentcfg` | id ของ **ชิ้นส่วน** (อาวุธ/หมวก/ผม/เครื่องประดับ) → bundle + skin + วิธีติดตั้ง (`followSlot`, `cmpRootBone`) | 6,174 (มี `followSlot` 311 แถว) |

ตัวละครหนึ่งตัว = `dbbodyconfig` 1 แถว + `dbcomponentcfg` หลายแถว (จำนวนขึ้นกับข้อมูลผู้เล่น/ตัวละคร)
การโหลดทำที่ `ActionSystem/SpineActor.cs` ซึ่งมี 2 โหมดต่อชิ้นส่วน:
1. **merge skin เข้ากับ body** (ชิ้นส่วนที่ `followSlot` ว่าง) — ไม่เพิ่ม renderer
2. **mount เป็น sub-skeleton** (ชิ้นส่วนที่มี `followSlot`) — เพิ่ม renderer 1 ตัวต่อชิ้น

---

## 1. `dbbodyconfig` — body/hull (Observed)

โครงสร้าง (`extracted/ilspy/Assembly-CSharp/ConfigData/dbbodyconfig.cs`):

| field | ความหมาย |
|---|---|
| `id` | body id (ค่าที่เกมส่งเข้ามาเป็น `bodyID`) |
| `name` | ชื่อ (ส่วนใหญ่เป็น `""`; เช่น `Body_461001_1`) |
| `noCmp` | `true` = **ไม่รับ** component ใด ๆ (`SpineActor` จะ log error ถ้ามีการส่งมา) |
| `spineAb` | bundle หลัก เช่น `character/spinelib/body/1113002_1_skeletondata.ab` |
| `s_spineAb` | bundle ตัวย่อ/ความละเอียดต่ำ ใช้เมื่อ `small = true` (672 แถวมีค่านี้) |
| `overrideSpine` | ให้เพิ่ม `SkeletonUtility` แบบ override transform |
| `spineSkins` | ชื่อ skin ที่ต้องรวมกันเป็นตัวละคร (มักเป็น `body_*`, `face_*`, `head_*`, `bg_*`) |
| `cmps` | component id ที่ผูกมากับ body นี้ตลอด (มีเพียง 16 แถว — ส่วนใหญ่เป็นสิ่งก่อสร้าง) |
| `shadow` | `[scaleX, scaleY, ?, ?]` ของเงา |
| `size` | `[w, h]` ขนาดตัว (ใช้คำนวณ HUD/หัว) |
| `headPos` | ตำแหน่งหัวสำหรับ HUD |

ตัวอย่างจริง (ยืนยันแล้วจากการ parse):

```jsonc
// 1113002 = ตัวเต็มตัวที่ใช้ในเดโม
{"id": 1113002, "noCmp": true,
 "spineAb": "character/spinelib/body/1113002_1_skeletondata.ab",
 "s_spineAb": "character/spinelib/body_s/1113002_1_s_skeletondata.ab",
 "spineSkins": ["body_1113002_1", "face_1113002_1", "head_1113002_1"],
 "cmps": [], "size": [0.59, 0.91], "shadow": [0.55, 0.28, 0, 0]}

// 2055..2059 = ตัวจิบิ 1123011 คนละ variant skin แต่ bundle เดียวกัน
{"id": 2055, "noCmp": false, "spineAb": "character/spinelib/body_npc/1123011_skeletondata.ab",
 "spineSkins": ["body_1123011_1", "head_1123011_1"]}
// 2056 → body_1123011_2 + head_1123011_2, ... 2059 → body_1123011_5 + head_1123011_5
```

> สังเกต: ตัวจิบิใช้ bundle เดียวกัน 5 แถว ต่างกันแค่ `spineSkins` → **คู่ที่ถูกต้องคือ `body_N` + `head_N` (N = 1..5)**
> ส่วน `..._6_skeletondata.ab` เป็นอีก bundle (ชุดต่างหาก) ไม่ใช่ variant ของ bundle นี้

สรุปจำนวนตามโฟลเดอร์ bundle (จาก 4,463 แถวที่มี `spineAb`): `ui` 764, `ui_qipaokuang` 340, `build` 319,
`body_shizhuang` 253 (ชุดแต่งตัว/สกิล), `build_jingguan` 228, `body_xianmo` 200, `monster` 188,
`body_npc` 101, `halfbody_dizi_special` 90 …
bundles ที่ถูกใช้มากกว่า 1 แถว: 460 bundle (สูงสุด 22 แถว/bundle) — กลไก "bundle เดียว หลาย variant"

---

## 2. `dbcomponentcfg` — ชิ้นส่วน (Observed)

โครงสร้าง (`ConfigData/dbcomponentcfg.cs`):

| field | ความหมาย |
|---|---|
| `id` | component id (ส่งเข้ามาใน `slots[]` ตอน `LoadBody`) |
| `slots` | slot ปลายทางใน skeleton ปลายทาง (ใช้กับอีกโหมดของ `DragonBonesActor`) |
| `spineAb` / `s_spineAb` | bundle ของชิ้นส่วน (มี `s_` สำหรับโหมด small) |
| `spineSkins` | skin ของชิ้นส่วน มักเป็น `group/name` เช่น `w_jian/w_jian_300108` |
| `followSlot` | **ถ้ามีค่า** → mount เป็น sub-skeleton ที่ slot นี้ (311 แถว) |
| `cmpRootBone` | bone รากของ sub-skeleton (ถ้าว่างใช้ bone ของ `followSlot`) |
| `listenAnimEvent` | ให้ sub-skeleton รับ animation event |
| `jsonData` | `useSrcOffset` (ค่าย้าย offset จากชิ้นส่วน) |

ค่าที่พบใน `followSlot`: `hat_h`, `head`, `root`, `yan1`, `yan2`, `yan3`, `zhuguge1..5` (+`_1`)

ตัวอย่าง:
```jsonc
// อาวุธ: followSlot ว่าง → merge skin เข้า body (slot อาวุธมีอยู่ใน body อยู่แล้ว)
{"id": 300108, "spineAb": "character/spinelib/weapon/weapon_skeletondata.ab",
 "spineSkins": ["w_jian/w_jian_300108"], "followSlot": ""}

// เครื่องประดับ: followSlot = root → mount sub-skeleton
{"id": 63472, "spineAb": "character/spinelib/halfbody_player/accessory_2113019_19_12_3_jt_skeletondata.ab",
 "spineSkins": ["accessory_2113019_19_12_3"], "followSlot": "root"}
```

อาวุธทั้ง 163 แถวชี้ไปที่ bundle เดียว `character/spinelib/weapon/weapon_skeletondata.ab` แล้วเลือก skin
(`w_jian/...`, `w_dao/...`, `w_shan/...`) → **นี่คือเหตุผลที่ body ของ 1113002 มี slot อาวุธ 5 ช่อง**
(`w_jian`, `w_baozhu`, `w_fan`, `w_zhang`, `w_maobi`) ทั้งที่ bundle body ไม่มี attachment เหล่านั้น

---

## 3. ลำดับการทำงานในโค้ด (Observed)

`ActionSystem/SpineActor.cs`:

| บรรทัด | สิ่งที่เกิด |
|---|---|
| 240-258 | `StartLoadMain(mainSkinID, attachSkins)` → `GetDBBodyConfig(mainSkinID)`; เลือก bundle: `m_IsSmall && s_spineAb != "" ? s_spineAb : spineAb`; สร้าง `SkinReq(mainSkinID, spineSkins, assetAb, …)` |
| 259-280 | ถ้า `!noCmp`: โหลด component จาก `attachSkins[]` (ส่งมาจากภายนอก) **และ** จาก `cmps[]` ของ body เอง; ถ้า `noCmp` แต่มี attachSkins → `LogError("spine身体{id}不接受组件")` |
| 290-309 | `AddAttachmentSkinReq(subSkinID)` → `GetDBComponentcfg(id)`; ถ้าไม่มี `spineSkins` → error; สร้าง `SkinReq(id, skins, ab, followSlot, cmpRootBone, …)` |
| 493-560 | `BuildBody()`: ถ้าไม่มี attach → `CreateSkeletonSkinWithNoCmp` (รวม skin ของ body) ; ถ้ามี → สร้าง `Skin("base_skin")`, `AddSkin` จาก body's skins, แล้ววน attach: ถ้ามี `followSlot` → `LoadSkeletonAttachment(...)`, ถ้าไม่มี → `AddSkin` เข้า base skin |
| 2113-2128 | `LoadSkeletonAttachment`: หา `Slot` ตาม `followSlot` แล้วใช้ **bone ของ slot นั้น** (`value.Bone.Data.Name`) หรือ `cmpRootBone` ถ้าระบุมา เป็นจุดเกาะของ sub-skeleton |
| 713 | `LoadBody(bodyID, slots, isGUI, scale, …)` — signature ที่เรียกจากภายนอก |

จุดที่ค่าถูกส่งเข้ามา (ไม่ใช่คำนวณในตาราง):
- `ActionSystem/SpineActor.cs:713` `LoadBody(int bodyID, int[] slots, …)`
- `ActorSystem/DragonBonesActor.cs:571` path เดียวกันสำหรับ DragonBones
- `airMiniGame/BaseEntity.cs:736` `CreateSpine(bodyID, slots, …)`
- `Lua/AirEntityManagerEx.cs:410` → `BaseEntity_CreateSpine` (Lua → C#)

**สรุป: `bodyID` และ `slots[]` มาจากข้อมูลเกม (Lua/เซิร์ฟเวอร์/ตาราง NPC-monster) ไม่ได้ derive จาก character id ในสองตารางนี้**

---

## 4. แผนผังการเลือกไฟล์

```
character / NPC / disciple data (Lua/เซิร์ฟเวอร์)
        │  bodyID + slots[]
        ▼
dbbodyconfig[bodyID] ──► spineAb (หรือ s_spineAb เมื่อ small)
        │                 spineSkins[]  (body_/face_/head_/bg_)
        │                 cmps[]        (component id เพิ่มเติม)
        ▼
dbcomponentcfg[cmpID] ──► spineAb + spineSkins[]
        │
        ├─ followSlot == ""  → merge skin เข้า skeleton หลัก  (renderer เท่าเดิม)
        └─ followSlot != ""  → mount sub-skeleton ที่ bone ของ slot นั้น (renderer +1)
```

---

## 5. เทียบกับโปรเจกต์ my_clone

| เกม | my_clone (ที่มีอยู่) |
|---|---|
| `spineSkins[]` รวมกันเป็น skin เดียว (`AddSkin`) | `Skeleton.SetSkin(new Skin(...))` + `AddSkin` (เดโมทำอยู่แล้วใน `SpineDemoController.ApplyCombinedSkin`) — `SpineActorBase.SetSkin(string)` รองรับทีละชื่อเท่านั้น |
| `followSlot` + `cmpRootBone` → sub-skeleton | `SpineActorBase.Mount(data, skin)` + `MountToBone(boneName)` (BoneFollowerGraphic) — ต้องแปลง slot → ชื่อ bone เอง (หรืออ่านจาก skeleton) |
| `s_spineAb` เมื่อ small | ต้องโหลด bundle `_s_` แยก (ยังไม่มีในเดโม) |
| `noCmp` | ถ้า `true` ห้ามใส่ component (เดโมไม่มี component อยู่แล้ว) |

> ตัวเต็มตัว `1113002` มี `noCmp: true` และ `cmps: []` → ในโปรเจกต์เราจึงได้ **1 renderer** ตามที่ควรเป็น
> ตัวเลข "5 renderer" ในเกมต้องมาจากชิ้นส่วนที่ `followSlot` ไม่ว่าง (แต่ละชิ้น = sub-skeleton 1 ตัว) หรือจากหลาย material page

---

## 6. ตารางที่ให้ `bodyID` + `slots[]` ต่อตัวละคร (Observed — พบแล้ว)

`bodyID` และ `slots[]` ไม่ได้มาจาก `dbbodyconfig`/`dbcomponentcfg` โดยตรง แต่มาจาก **ตาราง appearance**
ที่อยู่ฝั่ง Lua (`extracted/ab_text/data/config.*.txt`):

```
NPC:      npcimageconfig[npcImgId].image = [bodyIdIn?, [hairId, faceId, bodyImageId, accessoryId, weaponId]]
Disciple: discipleconfig[diziId].imagelib[3] = bodyImageId
                │
                ▼
        disciplebodyimage[bodyImageId]
                │  out_side            → bodyID (dbbodyconfig)
                │  in_side             → bodyID ของครึ่งตัว/ในร่ม
                │  clothing_out_side   → { clothingId: [bodyId(ไม่เต็มดาว), bodyId(เต็มดาว)] }
                │  skeletonID / skeletonID_lihui / skeletonID_lihui_notBg
                │  xm_transfer          → bodyID ตอนเป็น "心魔"
                ▼
        components (slots[]) =
          disciplehairimageconfig[hairId].out_side
        + disciplefaceimageconfig[faceId].out_side
        + disciplefaceaccessoryimageconfig[accId].out_side
        + discipleweaponimageconfig[weaponId].out_side   (ข้ามถ้า dbbodyconfig.jsonData.hideWeapon)
                ▼
        dbcomponentcfg[cmpId] → bundle + skins + followSlot
```

หลักฐานในโค้ด/สคริปต์เกม:

| ที่ | บรรทัด | เนื้อหา |
|---|---|---|
| `extracted/ab_text/binary/gamesys.npc.npcmodel.txt` | 846–905 | `getImageInfoOutSide()`: `bodyCfg.out_side`, hair/face/weapon `out_side` → `result.componets` |
| `extracted/ab_text/binary/gui.windows.disciple.uidisciplemodel_model.txt` | 366–... | `getDiscipleOutsideModelInfoByData()`: body + `clothing_map`/`clothing_out_side`/`xm_transfer`/`out_tm` + face/hair components |
| `extracted/ab_text/binary/gamesys.clothingsystem.clothinghelper.txt` | 321–326 | `cfg_discipleconfig_get[diziId].imagelib[3]` → `cfg_disciplebodyimageconfig_get` |
| `extracted/ab_text/binary/gamesys.systemzongmen.systemzongmencontroller_outgoer.txt` | 62–90 | ประกอบ outgoer ในฉากสำนัก: `modelParams.body` + `modelParams.componets` + weapon `out_side` → `WorldEntitySetting` |
| `EntitySystem/Entity.cs` | 275–300 | `InitDragon(bodyID, slots, …)` → `SpineActor.LoadBody(bodyID, slots, …)` |

จำนวนข้อมูล (parse แล้ว): `disciplebodyimageconfig` 480 แถว, `npcimageconfig` 525 แถว (259 แถว resolve ได้ ≥1 component),
`disciplehairimageconfig` 3,148 แถว, `discipleweaponimageconfig` เล็ก (skin อาวุธทุกอันอยู่ใน `weapon_skeletondata.ab`)

### เครื่องมือที่เพิ่ม: `extracted/resolve_character_model.py`

อ่านตาราง Lua ข้างต้น (read-only) แล้วแปลงเป็น bodyID + component ids + bundle/skin:

```bash
python extracted/resolve_character_model.py --npc-image 2022        # NPC → body + components
python extracted/resolve_character_model.py --disciple <id>          # disciple → out_side + clothing
python extracted/resolve_character_model.py --body-image 60108        # ดูแถว bodyimage ดิบ
python extracted/resolve_character_model.py --find-body 1113002       # หา bodyImage ที่ชี้ไป body นี้
```

ตัวอย่างผลจริง:

```
$ --find-body 1113002
bodyimage rows with out_side == 1113002: [60002, 60108]
bodyimage 60108: clothing_out_side={54401:[158131,158132], 54402:[158133,158134]},
                 in_side=[47002], out_side=1113002, skeletonID=2113002,
                 skeletonID_lihui=60006, skeletonID_lihui_notBg=63003

$ --npc-image 2022
image=[2113002, [60002, 0, 60002, 0]]
  body 1113002
    bundle : character/spinelib/body/1113002_1_skeletondata.ab
    skins  : [body_1113002_1, face_1113002_1, head_1113002_1]  noCmp=True
```

즉 NPC image 2022 = ตัวเต็มตัว 1113002 (ไม่มี hair/face/weapon component เพิ่ม),
ส่วนตัวที่มี component จะได้ `(kind, cmpId)` เช่น `('hair', 205012)` แล้วเอา cmpId ไปเปิดใน `dbcomponentcfg`

### ข้อจำกัดของชุดเกมที่ติดตั้ง

- `dbbodyconfig` อ้าง bundle 4,463 รายการ แต่ในเครื่องมีจริง **30** bundle (`StreamingAssets/Windows/character/spinelib`)
- `dbcomponentcfg` ที่มี `followSlot` 311 แถว → **ไม่มี bundle ใดอยู่ในเครื่องเลย (0)**
- มี bundle พร้อมใช้: body/1113002_1, body_npc/1123011, weapon/weapon_skeletondata (2048×364) ฯลฯ

---

## 7. สิ่งที่ยังไม่รู้ (Unknown) — ต้องเก็บเพิ่มถ้าจะประกอบตัวละครเฉพาะ

1. **id จริงของ NPC/disciple ที่จะประกอบ** — มาจากข้อมูลเกม (npc config/เซิร์ฟเวอร์) ไม่ได้อยู่ใน 2 ตารางนี้
2. **`clothingId` ที่ผู้เล่นสวมจริง** (คีย์ใน `clothing_out_side`) มาจากระบบ clothing/ไอเทม
3. **`useSrcOffset` / offset ต่อชิ้นส่วน** มีใน `jsonData` แต่ค่าจริงขึ้นกับแต่ละ component
4. **`s_spineAb` (small)**: โค้ดใช้เมื่อผู้เรียกส่ง `small = true` — ยังไม่ยืนยันว่าเกมเลือกตอนไหน
5. **NPC/monster config tables** (`npcimageconfig` มีแล้ว แต่ config ที่ให้ `npcImgId` ต่อ NPC ยังไม่ได้ extract)

---

## 8. ตัวอย่างการใช้งานจริงกับข้อมูลที่มี

```python
import json
body = {r['id']: r for r in json.load(open('extracted/dbbodyconfig.json', encoding='utf-8'))}
cmp_ = {r['id']: r for r in json.load(open('extracted/dbcomponentcfg.json', encoding='utf-8'))}

b = body[1113002]
print(b['spineAb'], b['spineSkins'], 'noCmp=', b['noCmp'])
for cid in b['cmps']:
    c = cmp_[cid]
    print(cid, c['spineAb'], c['spineSkins'], 'mount@', c['followSlot'] or '(merge skin)')
```
