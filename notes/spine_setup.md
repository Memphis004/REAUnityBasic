# Spine Unity 3.8 setup — my_clone

วันที่ตรวจ: 2026-10-23
โปรเจกต์ปลายทาง: `C:\Workspace\REA\my_clone`
แหล่งข้อมูลอ้างอิง (อ่านอย่างเดียว): `C:\Workspace\REA\game_copy\Immortal Clan Grandmastery_Data`

---

## 1. ผลตรวจเวอร์ชันข้อมูล Spine ของเกม

### 1.1 ไฟล์ skeleton ในเกม (Observed)

สแกน AssetBundle ใต้ `StreamingAssets\Windows` จำนวน 613 ไฟล์ พบ `TextAsset` ที่เป็น `.skel`
ทั้งหมด **30 ไฟล์** (ใต้ `Windows\character\spinelib\**`) และ **ทุกไฟล์ระบุ version `3.8.75`**
ไม่พบไฟล์ `.json` ที่เป็น skeleton data (มีแต่ `.atlas` และ `.skel`)

โครงสร้างไบนารีที่อ่าน: `<length 0x1C><hash 28 chars><0x07>3.8.75...`
(ส่วนหัวหลัง hash ตามสเปก SkeletonBinary)

ตัวอย่างที่ตรวจ (ถอดจาก .ab ด้วย UnityPy แบบอ่านอย่างเดียว):

| ไฟล์ใน bundle | ขนาด .skel | version | bones | slots | skins | animations | ตีความ |
|---|---:|---|---:|---:|---:|---:|---|
| `body\1113002_1_skeletondata.ab` → `1113002_1.skel` | 539,379 B | 3.8.75 | 77 | 25 | 4 | 24 | ตัวละครเต็มตัว |
| `ui\body_ui_zhandou_skeletondata.ab` → `Body_UI_zhandou.skel` | 10,315 B | 3.8.75 | 17 | 7 | 1 | 2 | ตัวจิบิ/UI ขนาดเล็ก |
| `biaoqing\biaoqing_skeletondata.ab` → `biaoqing.skel` | 1,108 B | 3.8.75 | 1 | 2 | 13 | 6 | ชุด表情 (expression) |
| `halfbody_npc_singleskeleton\2113002_skeletondata.ab` → `2113002.skel` | 221 B | 3.8.75 | 1 | 1 | 1 | 1 | ครึ่งตัว NPC |

ตัวเลข bones/slots/skins/animations เป็นผลจาก parse จริงด้วย runtime ที่ติดตั้ง (ดูข้อ 3)

### 1.2 Version resource ของ DLL ในโฟลเดอร์ Managed (Observed)

| ไฟล์ | ขนาด | FileVersion | ProductVersion | AssemblyVersion | MVID |
|---|---:|---|---|---|---|
| `spine-unity.dll` | 318,464 B | 0.0.0.0 | 0.0.0.0 | 0.0.0.0 | `62f2794f-7a99-4fcd-b3da-25cbbdc87bc0` |
| `spine-timeline.dll` | 12,288 B | 0.0.0.0 | 0.0.0.0 | 0.0.0.0 | `1ee3ac43-2921-4da6-99b0-d9891726e82b` |

DLL ไม่มี version resource จึงเทียบเลขรุ่นตรง ๆ ไม่ได้ หลักฐานเชิงโครงสร้างที่อ่านได้:

- `spine-unity.dll`: 218 types / 1,970 methods; refs `netstandard`, `UnityEngine.CoreModule`,
  `UnityEngine.AnimationModule`, `UnityEngine.UIModule`, `UnityEngine.Physics2DModule`,
  `UnityEngine.PhysicsModule`, `UnityEngine.UI`
- มี type ชุด 3.8 เช่น `Spine.TwoColorTimeline`, `Spine.SkeletonBinary`, `Spine.Unity.SkeletonGraphic`,
  `Spine.Unity.BlendModeMaterials`, `Spine.Unity.SkeletonDataCompatibility`
- มี API รุ่น 2020–2021 อยู่ใน DLL เช่น `SkeletonGraphic.allowMultipleCanvasRenderers`,
  `BeforeApply`, `updateWhenInvisible` → **ไม่ใช่ runtime เดือน ธ.ค. 2019**
- **ไม่มี string `3.8.75`, `3.8`, หรือ `Unsupported skeleton data` ใน DLL** (ตรวจ byte-level)

### 1.3 สรุปความสอดคล้อง (Inferred)

- ข้อมูลเกมเป็น **3.8.75** ทั้งหมด และอยู่ในสาย 3.8.x จึงไม่ต้องหยุดตามเงื่อนไขในโจทย์
- DLL เกมเป็น runtime สาย 3.8 (ไม่ใช่ 4.x) และใหม่กว่า 3.8.75 มาก (น่าจะ 3.8.9x)
  แต่ถูกตัด/แก้ hard-coded guard ที่ปฏิเสธข้อมูล 3.8.75 ออก
- ข้อสรุปนี้สำคัญมาก: **spine-csharp 3.8 ทางการทุกวันนี้ (รวม tag 3.8.95) มี guard
  `if ("3.8.75" == skeletonData.version) throw ...`** ซึ่งจะทำให้โหลดข้อมูลเกมไม่ได้
  (ดูข้อ 3.2)

---

## 2. Runtime ที่ติดตั้ง

| รายการ | ค่า |
|---|---|
| แหล่ง | `https://github.com/EsotericSoftware/spine-runtimes` |
| Branch | `3.8` |
| Commit ที่ใช้ | `8b4844bd4b193ba9e54497ed397a777993cbad56` |
| วันที่ commit | 2026-05-20 |
| Tag | ไม่มี tag ชี้ที่ HEAD (`git describe --tags` → `8b4844b`); tag `3.8.95` มีอยู่แต่เป็นคนละ commit และมี guard เดียวกัน |
| วิธี clone | `git clone --branch 3.8 --depth 1` ไปโฟลเดอร์ชั่วคราวนอก `my_clone` |

### 2.1 โฟลเดอร์ที่คัดลอกลง `my_clone`

```
C:\Workspace\REA\my_clone\Assets\Spine\
├─ Runtime\
│  ├─ spine-csharp\          42 .cs  (จาก spine-csharp\src)
│  ├─ spine-unity\           55 .cs + Shaders 37 ไฟล์ + Modules\TK2D (guard #if SPINE_TK2D)
│  ├─ spine-unity.asmdef
│  └─ Runtime.meta
├─ Editor\
│  ├─ spine-unity\           44 .cs
│  └─ spine-unity-editor.asmdef
├─ version.txt               "works with data exported from Spine Editor version: 3.8.xx"
├─ package.json              com.esotericsoftware.spine.spine-unity 3.8.0
└─ LICENSE.txt
```

รวม 492 ไฟล์ใน `Assets\Spine` (+ `Assets\Spine.meta`) / เป็น `.cs` 141 ไฟล์ / `.shader` 37 ไฟล์

**ไม่ได้คัดลอก**
- `Spine Examples` และ `SpineTests`
- `spine-unity\Modules\com.esotericsoftware.spine.timeline` (โปรเจกต์ยังไม่ได้ติดตั้ง Timeline)
- `spine-unity\Modules\com.esotericsoftware.spine.urp-shaders` และ `lwrp-shaders`
- DLL ใด ๆ จากโฟลเดอร์เกม (`my_clone` ไม่มีไฟล์ `.dll` เลย — ตรวจแล้ว 0 ไฟล์)

### 2.2 Patch ที่จำเป็น (ต้องรายงานให้ชัด)

ไฟล์ `Assets\Spine\Runtime\spine-csharp\SkeletonBinary.cs` ถูกแก้ 1 จุด
(SHA-256 ของไฟล์หลังแก้: `6F43D719F988C735B48F8A49825496C8C9BEB659A2C4762E4F8BC59B0AE42E9D`)

```diff
- if ("3.8.75" == skeletonData.version)
-     throw new Exception("Unsupported skeleton data, please export with a newer version of Spine.");
+ // Local compatibility patch (see notes/spine_setup.md):
+ // the game exports binary .skel data with version 3.8.75, which upstream
+ // spine-csharp 3.8 deliberately rejects here. The data parses correctly
+ // with this runtime, so the hard-coded rejection is disabled.
+ // if ("3.8.75" == skeletonData.version)
+ //     throw new Exception("Unsupported skeleton data, please export with a newer version of Spine.");
```

เหตุผล: ถ้าไม่แก้ ทุก skeleton ของเกมจะ throw ทันทีตอนโหลด แม้ข้อมูลจะ parse ได้ปกติ
(ทดสอบแล้วทั้งก่อนและหลังแก้) การแก้เป็นฝั่ง runtime ที่เราติดตั้งเอง ไม่ได้แตะ DLL ของเกม

---

## 3. การตรวจสอบ

### 3.1 ข้อจำกัดของ `my_clone`

`my_clone` **ยังไม่เป็น Unity project** (ไม่มี `ProjectSettings\ProjectVersion.txt`,
`Packages\manifest.json`, `Assets` เดิม) และเครื่องนี้ไม่มี Unity 2021.3 LTS
ติดตั้งอยู่ (มีเฉพาะ `2022.3.62f2` และ `6000.3.9f1`) จึงเปิด `my_clone` ด้วย batch mode ไม่ได้

### 3.2 Compile + parse test (Observed)

สร้างโปรเจกต์ทดสอบแยกต่างหากนอก `my_clone`:
`C:\Users\memph\AppData\Local\Temp\opencode\spine-compile-test`
ด้วย Unity **2022.3.62f2** (ไม่ใช่ 2021.3) เพิ่ม `com.unity.ugui` แล้วคัดลอก
`Assets\Spine` จาก `my_clone` ไปทั้งก้อน (ไฟล์เดียวกันกับที่ส่งมอบ)

ผลรัน `-batchmode -quit -nographics`:

- **compile ผ่าน 0 error** (exit code 0)
- warning เท่านั้น: `TextureImporter.spritesheet` obsolete ใน editor inspector (2022.3)
- รัน `SkeletonBinary.ReadSkeletonData` กับ 4 ตัวอย่างที่ถอดจากเกม:

```
[SMOKE] biaoqing.skel              version=3.8.75 bones=1  slots=2  skins=13 animations=6
[SMOKE] chibi_Body_UI_zhandou.skel version=3.8.75 bones=17 slots=7  skins=1  animations=2
[SMOKE] full_1113002_1.skel        version=3.8.75 bones=77 slots=25 skins=4  animations=24
[SMOKE] halfbody_2113002.skel      version=3.8.75 bones=1  slots=1  skins=1  animations=1
```

ก่อน patch guard: รันชุดเดียวกันได้ `Exception: Unsupported skeleton data, please export with a
newer version of Spine.` ทุกไฟล์ → ยืนยันว่าปัญหาอยู่ที่ guard ไม่ใช่ข้อมูลเสีย

### 3.3 Render pipeline / shader

`my_clone` ไม่มี `ProjectSettings\GraphicsSettings.asset` หรือ `QualitySettings.asset`
→ **ยังระบุ pipeline ไม่ได้ (Unknown)**

- ถ้าเป็น **Built-in RP**: ใช้ shader ที่ติดตั้งมาแล้ว
  - ตัวละคร 3D/2D mesh: `Spine/Skeleton` (default) หรือ `Spine/Skeleton Lit`
  - UI (SkeletonGraphic): `Spine/SkeletonGraphic` / `Spine/SkeletonGraphic Tint Black`
  - พร้อม outline/tint/blend mode ครบใน `Assets\Spine\Runtime\spine-unity\Shaders`
- ถ้าเป็น **URP**: ต้องติดตั้ง UPM module `com.esotericsoftware.spine.urp-shaders`
  จาก repo branch 3.8 (`spine-unity\Modules\com.esotericsoftware.spine.urp-shaders`)
  แล้วใช้ชื่อ shader:
  - `Universal Render Pipeline/Spine/Skeleton`
  - `Universal Render Pipeline/Spine/Skeleton Lit`
  - `Universal Render Pipeline/Spine/Sprite`
  - 2D: `Universal Render Pipeline/2D/Spine/Skeleton Lit`, `.../2D/Spine/Sprite`
  - **ยังไม่ติดตั้งให้** เพราะยังไม่รู้ pipeline และ module นี้ประกาศ dependency
    `com.unity.render-pipelines.universal 7.1.5` (ยุค 2019.3) ต้องทดสอบกับ URP เวอร์ชันของ 2021.3 ก่อน
  - ข้อสังเกต: module 3.8 ไม่มี shader สำหรับ `SkeletonGraphic` แบบ URP
    (มีแต่ Skeleton/Sprite) → การใช้ SkeletonGraphic บน URP ยัง **Unknown**

---

## 4. สิ่งที่ยังเหลือ / ความเสี่ยง

1. **สร้าง Unity project จริงที่ `my_clone`** ด้วย Unity 2021.3 LTS ก่อน
   - Unity Hub อาจไม่ยอมสร้างโปรเจกต์ใหม่ในโฟลเดอร์ที่ไม่ว่าง (มี `Assets\Spine` แล้ว)
   - ทางที่ปลอดภัย: สร้างโปรเจกต์ 2021.3 ที่อื่นแล้ว copy `Assets\Spine` (+ `Spine.meta`) เข้าไป
     หรือสร้างโปรเจกต์เปล่าใน `my_clone` ก่อนแล้วผมคัดลอกทับให้
2. **ยังไม่ได้รัน compile บน Unity 2021.3 จริง** — ที่ทดสอบคือ 2022.3.62f2
   (โค้ด 3.8 นี้อิง C# รุ่นเก่า คาดว่า 2021.3 ผ่าน แต่ยังเป็น inferred)
3. **Patch guard 3.8.75** เป็นการแก้ source ทางการ 1 จุด — ถ้าอัปเดต runtime ต้อง patch ใหม่
   หรือ re-export ข้อมูลด้วย Spine 3.8.76+ (ทำไม่ได้เพราะไม่มีไฟล์โปรเจกต์ต้นฉบับของเกม)
4. **URP shader module ยังไม่ติดตั้ง** และยังไม่รู้ pipeline ของโปรเจกต์
5. **Timeline ไม่ได้ติดตั้ง** ตามคำสั่ง (เกมมี `spine-timeline.dll` แต่ห้ามคัดลอก)
6. **ยังไม่ได้ติดตั้ง/import ข้อมูล skeleton, atlas, texture, material ของเกม**
   (รอบนี้เป็นเพียง runtime; ต้องตั้งค่า Texture Type = Texture และ Premultiplied Alpha
   ให้ตรงกับ shader `Spine/Skeleton` ภายหลัง)
7. ไม่มีการเขียนโค้ดระบบตัวละครใด ๆ ในรอบนี้ (ตามข้อ 5)

---

## 5. Observed / Inferred / Unknown

### Observed
- `.skel` 30 ไฟล์ใน `character\spinelib` ระบุ `3.8.75` ทุกไฟล์; ไม่พบ JSON skeleton
- ตัวอย่าง 4 ไฟล์ parse ได้จริงด้วย runtime ที่ติดตั้ง (bones/slots/skins/animations ตามตาราง)
- `spine-unity.dll` / `spine-timeline.dll`: FileVersion = ProductVersion = AssemblyVersion = 0.0.0.0
- `spine-unity.dll` 218 types / 1,970 methods; ไม่มี string `3.8.75` หรือ `Unsupported skeleton data`
- Branch 3.8 HEAD = commit `8b4844bd...` (2026-05-20), ไม่มี tag
- Source ทางการ 3.8 (HEAD และ tag 3.8.95) มี guard ปฏิเสธ `3.8.75`
- หลัง patch: compile 0 error และ parse 4/4 ตัวอย่างผ่าน บน Unity 2022.3.62f2
- `my_clone` ไม่มี ProjectSettings/ไฟล์ `.dll`; เครื่องไม่มี Unity 2021.3

### Inferred
- DLL เกมเป็น runtime สาย 3.8 รุ่นใหม่ (น่าจะ 3.8.9x) ที่ตัด guard ออก เพราะมี API ปี 2020–2021
  และ type surface ต่างจาก HEAD เฉพาะ types ที่ auto-generated/compiler-generated
  (`MonoScriptData`, `UnitySourceGeneratedAssemblyMonoScriptTypes_v1`, static array init)
  กับ `Spine.Unity.MaterialChecks` (ไม่มีใน DLL เกม)
- ข้อมูล 3.8.75 เข้ากันได้กับ parser 3.8 HEAD ในทางโครงสร้าง (guard เป็นการบล็อกเชิงนโยบาย)
- ตัวอย่าง `Body_UI_zhandou` เป็นตัวจิบิ และ `1113002_1` เป็นตัวเต็ม (ดูจากชื่อ/ขนาด/จำนวน bone)
- ติดตั้งลง `Assets\Spine` แล้วจะใช้ได้กับทั้ง Built-in และ (หลังเพิ่ม module) URP

### Unknown
- Render pipeline ของ `my_clone` (ยังไม่มีโปรเจกต์)
- Runtime version ที่แท้จริงของ DLL เกม (ไม่มี version resource)
- ความเข้ากันได้ของ URP shader module 3.8 กับ URP เวอร์ชันใน Unity 2021.3
- วิธี render `SkeletonGraphic` บน URP ด้วย runtime 3.8 (module ไม่มี shader ให้)
- ผล compile บน Unity 2021.3 จริง (ทดสอบได้เพียง 2022.3.62f2)
- พฤติกรรม runtime กับ skeleton ทั้ง 30 ไฟล์ (ทดสอบเชิงลึก 4 ไฟล์)
