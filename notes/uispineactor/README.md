# `ActionSystem.UISpineActor` — ชุดเอกสารพฤติกรรม (Phase 3)

เอกสารชุดนี้อธิบาย **พฤติกรรมที่สังเกตได้** และ **dependency** ของ `ActionSystem.UISpineActor`
ในเกม `Immortal Clan Grandmastery` เพื่อใช้เป็นสเปกตั้งต้นในการทำ implementation ที่เข้ากันได้ใน `my_clone`
โดยไม่คัดลอกซอร์ส decompile หรือ asset ของเกม

> ขอบเขต: managed code ใน `Assembly-CSharp.dll` (+ `Assembly-CSharp-firstpass.dll` เมื่อจำเป็น)
> ไม่รวม asset/shader/prefab จริงของเกม และไม่รวมการแก้ไขเกมที่ติดตั้ง

---

## 1. สถานะของหลักฐาน

ใช้ป้ายกำกับทุกข้อความเชิงพฤติกรรม:

| ป้าย | ความหมาย |
|---|---|
| **Observed** | อ่านได้ตรงจาก metadata/CIL หรือ decompile (มี file:line หรือ token/IL size) |
| **Derived** | สรุปจากการรวม Observed หลายจุด (ระบุจุดอ้างอิง) |
| **Inferred** | ตีความจากชื่อ/บริบท **ยังไม่มีหลักฐานตรง** |
| **Unknown** | ยังไม่มีหลักฐาน ห้ามเติมเอง — ดู `UNKNOWNS.md` |

---

## 2. Artifact ที่ใช้อ้างอิง

| รายการ | ค่า |
|---|---|
| `Assembly-CSharp.dll` | SHA-256 `62761be23a4a40518eeb0e98a2d6be6e4b11c3b1bad2064e4c8b528d9876184f`, 5,151,744 B, PE x86 |
| MVID | `804d1d3b-ea79-4872-9784-771b88e440f4` (metadata v4.0.30319, complete) |
| TypeDef / MethodDef / MemberRef | 3,883 / 30,652 / 10,455 |
| `ActionSystem.SpineActor` | TypeDef `0x02000640` → `UnityEngine.MonoBehaviour` |
| `ActionSystem.UISpineActor` | TypeDef `0x02000643` → `ActionSystem.SpineActor` |
| `UIModelShow` | TypeDef `0x02000059` → `UnityEngine.MonoBehaviour` |

Type ที่เกี่ยวข้อง **ไม่ปรากฏ** ใน `Assembly-CSharp-firstpass.dll` (Observed จาก E1/E2 — ดู `EVIDENCE.md`)

---

## 3. แผนที่เอกสาร

| ไฟล์ | เนื้อหา | ใช้ตอนไหน |
|---|---|---|
| `README.md` | index, หลักฐาน, คำจำกัดความ | เริ่มอ่าน |
| `EVIDENCE.md` | evidence ID (E1–E15), การ map ข้อความ → หลักฐาน, วิธีตรวจซ้ำ | ตรวจย้อนกลับ |
| `BEHAVIOR.md` | signature/token/IL size/พฤติกรรมของทั้ง 48 เมธอด + พฤติกรรมที่สืบทอดจาก `SpineActor` | เขียนโค้ด |
| `CALLGRAPH.md` | เส้นทางเรียกจาก UI/Lua ถึง `UISpineActor`, virtual dispatch, sub-actor/mount/separator graph | วางโครงสร้าง |
| `DEPENDENCIES.md` | Spine fork API, `GUIConfig`, config/AB/effect service, Unity API, ช่องว่างกับ spine-unity 3.8 | ประเมินของที่ต้องมี |
| `LIFECYCLE.md` | ลำดับ create → load → build → update → recycle, ownership ของการ cleanup, timing hazards | ทำ memory/resource ให้ถูก |
| `API_COMPAT.md` | surface ที่ต้องมี, checklist ความเข้ากันได้, สถานะ MVP ปัจจุบัน | ตรวจความครบ |
| `UNKNOWNS.md` | สิ่งที่ยังไม่รู้ + วิธีทดสอบ runtime ที่ทำได้จริง | ก่อนสรุป design |
| `REPORT.md` | รายงาน Phase 1 (ภาพรวมกว้าง) | อ้างอิงเพิ่ม |
| `MYCLONE_DESIGN.md` | design เดิมของ clone | เทียบ |
| `IMPLEMENTATION_RESULT.md` | ผล MVP เดิม (17/17 tests) | เทียบสถานะ |

ไฟล์ข้อมูลดิบ:

- `ActionSystem_UISpineActor_methods.csv` — 48 เมธอด (token, signature, IL size, IL hash, callees)
- `ActionSystem_SpineActor_methods.csv` — 159 เมธอดของ base
- `UIModelShow_methods.csv` — 40 เมธอดของ host
- `members_subset.json` — members + 1,597 call edges + 1,530 field accesses
- `callsites.txt` — จุดเรียกที่พบทั้งโปรเจกต์ (grep จาก ILSpy output)

Decompilation ที่อ่าน:

- `extracted/ilspy/Assembly-CSharp/ActionSystem/UISpineActor.cs`
- `extracted/ilspy/Assembly-CSharp/ActionSystem/SpineActor.cs`
- `extracted/ilspy/Assembly-CSharp/UIModelShow.cs`
- `extracted/ilspy/Assembly-CSharp/UIModelCache/UIModelCacheItem.cs`
- `extracted/ilspy/Assembly-CSharp/CSGUI/GUIConfig.cs`
- `extracted/ilspy/spine-unity/Spine.Unity/SkeletonGraphic.cs` (fork ของเกม)

---

## 4. ข้อสรุปสั้นที่สุด (สำหรับตัดสินใจก่อนอ่านละเอียด)

**Observed:**

1. `UISpineActor` เป็น subclass ของ `SpineActor` ที่เปลี่ยน rendering จาก `SkeletonAnimation` + `MeshRenderer`
   เป็น `SkeletonGraphic` + `CanvasRenderer` โดย override hook ของ base
2. ภายนอกไม่เรียก `UISpineActor` โดยตรง ยกเว้น 2 จุดสร้าง (`UIModelShow.CreateActor`,
   `UIModelCacheItem`) และ 1 จุด cast (`UIModelShow.SetSlotAttachment`); ที่เหลือเดินผ่าน `IActor`
3. งานหลักของ subclass มี 3 กลุ่ม: (ก) สร้าง/ทำลาย `SkeletonGraphic` + material,
   (ข) sub-skeleton/separator/follow actor แบบ UI, (ค) mount แบบ UI
4. หลายเมธอดเป็น **hook ว่างหรือเกือบว่าง** โดยเจตนา: `DrawToBuffer`, `SetShaderType`,
   `UpdateAnimationState*`, `UpdateAnimationSpeed` (ของ base ว่าง; UI แตะเฉพาะ sub-skeleton)
5. `SetFreezeAnimation` ของ UI **ไม่มีผล** เพราะ base อ้าง `m_SkeletonAnimation` ซึ่งเป็น `null`
6. `m_RaycastTarget` ถูกอ่านตอนสร้าง SG แต่ **ไม่มีการเขียนค่าเลย** → `raycastTarget = false` เสมอ
7. `UISpineActor.Mount` เรียก `SeparatorSkeleton` บน mount ที่ยังโหลดไม่เสร็จ จึงคืน `false`
   แล้วจัด sibling แบบ fallback; งานแยก part จริงเกิดใน base `DoMount` (async) — ดู `CALLGRAPH.md` §7
8. Material 8 ตัวมาจาก `GUIConfig` (asset กลาง) ไม่ใช่ของที่ actor สร้าง → **ห้าม destroy**
9. `UISpineActor` พึ่ง API ที่เกมเพิ่มใน `SkeletonGraphic`: `SetSerparatorSlotNames`,
   `SeparatorParts`, `UpdateRectTransformSize`, `separatorSlots`, `enableSeparatorSlots`
   ซึ่ง **ไม่มีใน spine-unity 3.8 มาตรฐาน**
10. `StopAnimationState` ของ UI ไม่บันทึก `m_StateID`/`m_AnimationSpeed` (ต่างจาก base) และ
    ตั้ง `timeScale` ของ sub-skeleton กลับเป็น 1 → sub-skeleton อาจเล่นต่อหลัง "stop"

---

## 5. สิ่งที่เอกสารชุดนี้ *ไม่* ทำ

- ไม่มีซอร์ส decompile คัดลอกมาแบบยกไฟล์ (มีเฉพาะ signature สั้น ๆ และคำอธิบาย)
- ไม่ได้ยืนยันค่า asset จริง (shader/material/prefab) — อยู่ใน `UNKNOWNS.md`
- ไม่ได้แก้ `my_clone` ในเฟสนี้; `API_COMPAT.md` เทียบสถานะ MVP เดิมเท่านั้น
- ไม่ได้ทดสอบ runtime กับ build จริง; ข้อเสนอการทดสอบอยู่ใน `UNKNOWNS.md`
