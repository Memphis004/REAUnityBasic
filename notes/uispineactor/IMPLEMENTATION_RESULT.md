# ขั้นที่ 3 — ผลการ implement MVP (`my_clone`)

อ้างอิง design: `notes/uispineactor/MYCLONE_DESIGN.md`
สถานะ: **MVP 1–4 ผ่าน test, 5 = scaffold, 6 = ไม่ทำ**

---

## 1. ไฟล์ที่เพิ่ม

| ไฟล์ | หน้าที่ |
|---|---|
| `Assets/Scripts/SpineSystem/MyClone.SpineSystem.asmdef` | assembly `MyClone.SpineSystem` อ้าง `spine-unity` |
| `Assets/Scripts/SpineSystem/SpineActorBase.cs` | lifecycle, play/stop/progress/speed, flip, slot/skin, gray state, mount |
| `Assets/Scripts/SpineSystem/SpineUiActor.cs` | `SkeletonGraphic` + CanvasRenderer, material normal/gray, separator, mount UI |
| `Assets/Scripts/SpineSystem/SpineWorldActor.cs` | `SkeletonAnimation` + `MeshRenderer` + `SortingGroup`, MPB/skeleton color, gray override |
| `Assets/Scripts/SpineSystem/SpineSkeletonGraphic.cs` | เปิด `separatorSlotNames` ตอน runtime + `UpdateRectTransformSize()` |
| `Assets/Scripts/SpineSystem/SpineActorFactory.cs` | `CreateUi` / `CreateWorld` |
| `Assets/Scripts/SpineSystem/Shaders/Spine-SkeletonGraphic-Grayscale.shader` | UI grayscale shader (`Spine/SkeletonGraphic Grayscale`) |
| `Assets/Tests/SpineSystem/*` | EditMode tests + synthetic skeleton/atlas (ไม่ใช้ asset เกม) |

---

## 2. สิ่งที่ทำได้ (ตรงตาม MVP)

### ขั้น 1 — skeleton + mount
- `Build()` สร้าง `SpineSkeletonGraphic` ใต้ RectTransform, `Release()` ทำลายทิ้ง, `Build()` ซ้ำได้
- `Mount(data, skin)` สร้าง/ใช้ `SpineMount` เป็นลูก, `UnMount()` แค่ `SetActive(false)`,
  `DestroyMount()` ทำลายจริง (ตรงกับพฤติกรรมเดิมของเกม)
- `MountToBone(name)` ใช้ `BoneFollowerGraphic` / `BoneFollower`

### ขั้น 2 — animation
- `Play(name, loop, speed)`, `Stop()/Pause()`, `Resume()`, `SetSpeed()`, `SetProgress(0..1)`, `Tick(dt)`
- `AnimationComplete` ยิงครั้งเดียวเมื่อ non-loop จบ, ไม่ยิงสำหรับ loop
- **แก้ quirk ของเกม:** ค่า `speed` ถูกใช้จริง (เกมทิ้งค่า) และ animation ถูกตั้งที่ skeleton หลัก

### ขั้น 3 — flip + slot
- `SetFlipX/Y` ตั้ง `localScale` ของ transform ของ skeleton (±1)
- `SetSlotAttachment`, `ClearSlot`, `GetSlotAttachment`, `SetSkin`
- ตรวจชื่อ slot/attachment ก่อน แล้วคืน `false` + warning (เกมไม่ตรวจ — เราเพิ่ม error visibility)

### ขั้น 4 — gray
- `SetGray(true/false)` สลับ material instance ต่อ actor (ไม่แตะ asset ผู้ใช้)
- UI: `Spine/SkeletonGraphic Grayscale` (มี stencil/clip/CanvasGroup เหมือน stock)
- World: `CustomMaterialOverride` → `Spine/Special/Skeleton Grayscale`
- ถ้า shader หาย: `LogError` ชัดเจน + fallback เป็น normal (ไม่ crash)

### ขั้น 5 — scaffold
- `SetSeparatorSlots(params string[])` → `enableSeparatorSlots = true`, `allowMultipleCanvasRenderers = true`,
  `SpineSkeletonGraphic.SetSeparatorSlotNames(...)`
- **ยังไม่ทำ sub-skeleton / attach-skin** (`CreateSubSkeleton` ของเกม) — ต้องมีระบบ skin request ก่อน

### ขั้น 6 — ไม่ทำ
- `DrawToBuffer` / RenderTexture / CommandBuffer ไม่ implement
- เหตุผลจาก RE: `UISpineActor.DrawToBuffer` ว่างเปล่า (IL size 1) — RT เป็นของ `UIModelShow` + DragonBones/world

---

## 3. ผลการทดสอบ

รัน EditMode tests สำเร็จ **17/17 ผ่าน** (`passed=17 failed=0`)

วิธีรัน (กรณีเปิด editor อยู่และ MCP ไม่ว่าง): คัดลอก `Assets/Packages/ProjectSettings`
ไปยังโปรเจกต์ชั่วคราว แล้วรัน

```powershell
Unity.exe -batchmode -nographics -projectPath <temp-project> `
  -runTests -testPlatform EditMode -testResults <temp>/test-results.xml `
  -logFile <temp>/test-run.log
```

เคสที่ผ่าน:

| Fixture | จำนวน | ครอบคลุม |
|---|---|---|
| `SpineUiActorTests` | 14 | build/release/rebuild, play loop/non-loop, complete event, progress, speed, flip, slot/skin, gray, mount/unmount/destroy, mount-to-bone, separator |
| `SpineWorldActorTests` | 3 | build valid, play+flip, gray override |

หมายเหตุ: รอบแรกพบ 3 ปัญหาและแก้แล้ว
1. `Build()` เช็ค `IsBuilt` ก่อนตั้ง `m_Built` → renderer ถูกมองว่า invalid (แก้เป็นเช็ค `Skeleton/AnimationState`)
2. test atlas ต้องมีบรรทัดว่างนำหน้า ไม่งั้น `SpineAtlasAsset` หา page ไม่เจอ
3. Spine 3.8 JSON ใช้ `skins` เป็น array (ไม่ใช่ object)

---

## 4. ข้อจำกัดที่รู้ตัว (ไม่ปิดบัง)

1. **TintBlack + Gray**: grayscale shader ที่เพิ่มไม่มี TEXCOORD1/2 แบบ tint-black
   → ถ้าเปิด `tintBlack` และไม่ส่ง `grayMaterial` มาเอง จะเตือนและ gray ไม่ทำงาน
2. **Sub-skeleton**: ยังไม่มี (ขั้น 5 เฉพาะ scaffold)
3. **Separator sibling order**: ใช้พฤติกรรม stock `SkeletonGraphic` (parts เป็นลูกของ SG)
   ไม่ replicate การ reparent/sibling index ของเกม
4. **ไม่มี async loading / AB refcount**: ผู้เรียกจัดการ `SkeletonDataAsset` เอง
5. **ไม่มี flash/outline/shadow/raycast/HUD offset/soft mask** — อยู่นอกขอบเขต 3 คลาสที่ทำ RE
6. **Test artifact**: โปรเจกต์จริง (`my_clone`) ยังไม่ถูก import โดย editor ที่เปิดอยู่
   เพราะ MCP session หมดอายุกลางงาน; ไฟล์ผ่านการ compile และรัน test ในสำเนาสะอาดแล้ว

---

## 5. ขั้นถัดไป (ถ้าจะทำต่อ)

1. เปิด Unity ที่ `my_clone` แล้วปล่อยให้ import/compile (ครั้งแรกจะมี `.meta` ให้)
2. ถ้าต้องใช้ attach-skin: ออกแบบ `SkinRequest` + `CreateSubSkeleton` (ขั้น 5 ของ design)
3. ถ้าต้องใช้ RT: พิสูจน์ก่อนว่าต้องใช้ `DrawToBuffer` จริง — ตาม RE แล้ว UI-spine ไม่ได้ใช้
4. ถ้าต้องรองรับ tint-black + gray: ทำ shader grayscale แบบ tint-black (เพิ่ม TEXCOORD1/2) หรือบังคับส่ง `grayMaterial`
