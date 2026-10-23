# EVIDENCE — แหล่งที่มาของข้อความในชุดเอกสาร

เอกสารนี้รวม **evidence ID** ที่ใช้อ้างใน `README.md`, `BEHAVIOR.md` และเอกสารอื่น
เพื่อให้ตรวจย้อนกลับได้ว่าแต่ละข้อความเป็น Observed / Derived / Inferred / Unknown
โดยไม่ต้องเปิดซอร์ส decompile ซ้ำ

> หลักการ: ทุกข้อความเชิงพฤติกรรมต้องมีหลักฐานอย่างน้อยหนึ่งรายการ
> ถ้าไม่มี → ต้องอยู่ใน `UNKNOWNS.md` ไม่ใช่เติมเอง

---

## 1. ID ของหลักฐาน

| ID | ชนิด | ที่มา | ค่า / หมายเหตุ |
|---|---|---|---|
| **E1** | metadata | `inspect_managed_members` (selection summary, 3 types) | `ev_e6bfb4cd52a6be3d0f436501c62fea25129fa86004d9a20ed7712ca5e89331ec` |
| **E2** | metadata | `inspect_managed_members` (ทั้ง assembly, extract subset) | `ev_7d8b0cb6996072c714cd3523c83b3562e2b8fe1fe6bc389bafda007a21ef3b65` |
| **E3** | decompile | `extracted/ilspy/Assembly-CSharp/ActionSystem/UISpineActor.cs` | พฤติกรรมรายเมธอด, file:line ใน `BEHAVIOR.md` |
| **E4** | decompile | `extracted/ilspy/Assembly-CSharp/ActionSystem/SpineActor.cs` | base class hooks/lifecycle |
| **E5** | decompile | `extracted/ilspy/Assembly-CSharp/UIModelShow.cs` + `UIModelCache/UIModelCacheItem.cs` | external entry points, factory, cache |
| **E6** | decompile | `extracted/ilspy/spine-unity/Spine.Unity/SkeletonGraphic.cs` | fork API (`SetSerparatorSlotNames`, `UpdateRectTransformSize`, separator parts) |
| **E7** | decompile | `extracted/ilspy/Assembly-CSharp/CSGUI/GUIConfig.cs` | material fields `uiSpine*` |
| **E8** | stock compare | `my_clone/Assets/Spine/Runtime/spine-unity/Components/SkeletonGraphic.cs` (spine-unity 3.8) | ช่องว่าง fork vs stock — ดู `DEPENDENCIES.md` §4 |
| **E9** | derived data | `members_subset.json` | 1,597 call edges, 1,530 field accesses (จาก E1/E2) |
| **E10** | derived data | `ActionSystem_UISpineActor_methods.csv` | 48 เมธอด: token, signature, IL size, IL hash, callees |
| **E11** | derived data | `ActionSystem_SpineActor_methods.csv` | 159 เมธอดของ base |
| **E12** | derived data | `UIModelShow_methods.csv` | 40 เมธอดของ host |
| **E13** | grep | `callsites.txt` | จุดเรียกทั้งโปรเจกต์จาก ILSpy output |
| **E14** | runtime (จำกัด) | `myclone-test-run.log` / `myclone-test-results.xml` | ผล EditMode 17/17 ของ MVP เดิม (ไม่ใช่เกมจริง) |
| **E15** | static config (ไม่ยืนยันค่า) | `dbbodyconfig` / `dbcomponentcfg` / `dbanimationstate` | เห็นชนิด/ฟิลด์จาก decompile; **ค่าจริงเป็น Unknown** |

Artifact หลัก:

- `Assembly-CSharp.dll` — SHA-256 `62761be23a4a40518eeb0e98a2d6be6e4b11c3b1bad2064e4c8b528d9876184f`,
  5,151,744 B, PE x86
- MVID `804d1d3b-ea79-4872-9784-771b88e440f4` (metadata v4.0.30319, complete)
- TypeDef / MethodDef / MemberRef = 3,883 / 30,652 / 10,455

---

## 2. ข้อความสำคัญ → หลักฐาน

| ข้อความ | หลักฐาน | ระดับ |
|---|---|---|
| `UISpineActor : SpineActor` (TypeDef `0x02000643`) | E1, E2 | Observed |
| 48 เมธอด + token/IL size | E2, E10 | Observed |
| ลำดับ create → load → build → recycle | E3, E4, E5 | Observed (ลำดับจากโค้ด) |
| `DestroySkeleton` คืน `SeparatorParts` ก่อน destroy | E3, E6 | Observed |
| `CreateSpineComponent` ปิด `SortingGroup.enabled` | E3 | Observed |
| `raycastTarget` เป็น false เสมอ | E3 (ไม่มี write ถึง `m_RaycastTarget`) | Observed |
| `StopAnimationState` ไม่บันทึก `m_StateID`/`m_AnimationSpeed` | E3 เทียบ E4 | Observed |
| `m_Mat`/`m_GrayMat` ถูกเขียนทับทุก `CreateSkeletonGraphic` | E3 | Observed |
| PMA ถูกบังคับ false เมื่อ non-tint + canvas-group | E3 | Observed |
| `Mount` เรียก `SeparatorSkeleton` ก่อนโหลดเสร็จ | E3, E4 | Observed |
| `OnAttachmentLoaded` เรียก `ForceUpdate` ในลูป | E3 | Observed |
| `SetFlipY` ไม่ reset scale (ต่างจาก `SetFlipX`) | E3 | Observed |
| `UISpineActor` สร้างด้วยชื่อ GameObject `"SpineActor"` | E5 | Observed |
| material 8 ตัวมาจาก `GUIConfig` (ไม่ destroy) | E3, E7 | Observed (ชื่อ field) / Derived (ownership) |
| sub-skeleton + separator flow | E3, E6 | Observed |
| `AnimationState.SetAnimation(..., showErr)` ต่าง fork/stock | E6, E8 | Observed |
| ค่า `s_UISpineAlwaysMult` จริง | — | **Unknown (U01)** |
| shader/material asset จริง | — | **Unknown (U03/U13)** |
| พฤติกรรม runtime บน build จริง | E14 (เฉพาะ clone) | **Unknown สำหรับเกม** |

> ข้อความใดใน `BEHAVIOR.md` ที่ไม่มี ID กำกับ ให้ถือว่าอ้าง E3 โดยปริยาย
> (มี token + IL size + file:line ในหัวข้อนั้น)

---

## 3. วิธีตรวจซ้ำ

```powershell
# 1) นับและเทียบ 48 เมธอดกับตาราง §2.0
Import-Csv notes\uispineactor\ActionSystem_UISpineActor_methods.csv |
  Measure-Object | Select-Object -ExpandProperty Count   # ต้องได้ 48

# 2) ตรวจว่า token ในตารางตรงกับ CSV
#    (เทียบด้วยตา หรือใช้สคริปต์ compare ที่สร้างจากคอลัมน์ token)

# 3) ตรวจ call edges
python notes\uispineactor\extract_members.py   # สร้าง members_subset.json
python notes\uispineactor\analyze_callers.py   # สรุปผู้เรียก
```

การตรวจความครบของเอกสาร (ทำแล้วในรอบ consistency pass):

- §2.1–§2.8 มีหัวข้อ 1–48 ครบและไม่ซ้ำ (48/48)
- ตาราง §2.0 มี 48 แถว และคอลัมน์ § ชี้ไปหัวข้อที่มีอยู่จริง
- ตาราง Markdown ทุกไฟล์ไม่มีแถวที่จำนวนคอลัมน์ไม่ตรง
- code fence ทุกไฟล์ปิดครบคู่

---

## 4. ข้อจำกัด

- E1/E2 เป็น **metadata ของ build ที่ระบุ hash เท่านั้น** — token เป็น build-local
  ถ้าเกมอัปเดต ต้อง extract ใหม่และเทียบ hash ก่อนใช้ token เดิม
- E3–E8 เป็น decompile แบบอ่านอย่างเดียว; ไม่ได้ execute เกม
- E14 เป็นผลทดสอบของ `my_clone` ไม่ใช่ของเกม → ใช้ยืนยัน clone ไม่ใช่ยืนยันพฤติกรรมเกม
- ไม่มีการคัดลอกซอร์สหรือ asset ของเกมลงใน `my_clone`
