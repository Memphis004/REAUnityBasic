# UNKNOWNS — `ActionSystem.UISpineActor`

รายการนี้แยก **สิ่งที่ยังไม่มีหลักฐาน** ออกจากสิ่งที่ยืนยันแล้ว
พร้อมผลกระทบและวิธีทดสอบที่ทำได้จริงโดยไม่แก้เกมที่ติดตั้ง

หลักการ: ถ้าไม่มีหลักฐาน ให้คงเป็น **Unknown** และห้ามเติม assumption ลงในโค้ด
จนกว่าจะทดสอบได้

---

## 1. ตาราง Unknowns

| ID | Unknown | สิ่งที่รู้แล้ว (Observed) | ผลกระทบถ้าเดาผิด | วิธีทดสอบ |
|---|---|---|---|---|
| **U01** | ค่าจริงของ `s_UISpineAlwaysMult` | default ใน IL = `false`; ผูก `AppDataModelOption` key `option.ActionSystem.UISpineActor.s_UISpineAlwaysMult` | multi-renderer เปิด/ปิดผิด → separator/atlas หลาย material แสดงผิด | อ่าน player prefs/config ของ build; หรือ log ค่าหลัง `AppDataModel` init |
| **U02** | ค่าจริงของ `s_UISpineLoadPriority` | default 2 (`eMid`); `GUILoadBody` ใช้ค่านี้ | ลำดับโหลดต่างจากเกม (ไม่กระทบภาพสุดท้าย) | log ตอน `GUILoadBody` |
| **U03** | material asset จริงของ `GUIConfig.uiSpine*` 8 ตัว | มี 8 ฟิลด์; ชื่อชี้ไปที่ material asset นอก DLL | shader/feature (clip, tint, gray) อาจไม่ตรง | เปิด prefab ที่มี `GUIConfig` ใน game_copy; ดู material/shader |
| **U04** | ค่า serialized ของ `m_SeparatorBaseSortingOrder` | field default 0; `RecycleSelf` เขียน 5 แล้วทับด้วย 0 | sibling index ของ separator parts ผิด | ดูค่าใน prefab/instance; หรือ log ก่อน `SeparatorSkeleton` |
| **U05** | `useSrcOffset` กับ attachment ที่ไม่ใช่ `RegionAttachment` | cast ตรงเป็น `RegionAttachment` ทั้งคู่ | `InvalidCastException` ถ้าเป็น mesh attachment | หา config ที่ `useSrcOffset=true` + skin ที่มี mesh attachment; ทดสอบเปลี่ยน slot |
| **U06** | เจตนาของ `UpdateAnimationSpeed` ที่ทิ้งค่า | ตั้ง sub `timeScale = 1f` เสมอ | sub-skeleton เล่นด้วยความเร็วต่างจาก main | ตั้ง speed 0.5 แล้วดู sub vs main |
| **U07** | เจตนาของ `m_RaycastTarget` | ไม่มีการเขียนค่า → `raycastTarget=false` ตลอด | ถ้าต้องการให้ภาพรับ raycast ต้องเพิ่ม API | ตรวจ prefab/การเรียก; ปัจจุบันยืนยันได้แค่ว่า false |
| **U08** | animation ที่ได้จากการ cache (`UIModelCacheItem`) | เรียก `StopAnimationState` หลัง `LoadBody` ขณะ SG ยัง null → early return ไม่บันทึก state | ภาพ cache อาจเป็น animation เริ่มต้น (state 0) ไม่ใช่ที่ขอ | capture cache แล้วเทียบภาพกับ `StopAnimationState(animID,1f)` ตอนพร้อม |
| **U09** | ลำดับ separator ของ mount ในเกมจริง | `Mount` เรียก `SeparatorSkeleton` ก่อนโหลดเสร็จ → fallback; `DoMount` เรียกซ้ำเมื่อพร้อม | ลำดับ part/sibling ต่าง | log `SeparatorParts.Count` + sibling index ทั้ง 2 จุด |
| **U10** | พฤติกรรมเมื่อ skin name ไม่พบใน sub-skeleton | `FindSkin` อาจ null; `SetSkin(null)`; `SeparatorSkeletoGraphics` อ่าน `Skin.Attachments` | NRE หรือภาพว่าง | ทดสอบ skin ที่ตั้งใจให้ผิด; ดู exception |
| **U11** | ผลของการเรียก `ForceUpdate` หลายครั้ง | อยู่ในลูป `OnAttachmentLoaded`; `Update()` advance 1 frame | animation กระโดด/ไม่ตรง | เปลี่ยน attachment หลายชื่อแล้ววัดเวลา animation |
| **U12** | ผลภาพจริงของ material overwrite (`m_Mat`/`m_GrayMat`) | ถูกเขียนทุก `CreateSkeletonGraphic` รวม sub | gray/tint ผิดเฉพาะ some sub | สร้าง sub ที่ tintBlack ต่างกัน แล้ว `SetGray` |
| **U13** | render pipeline + shader ที่ build ใช้ | ไม่มีข้อมูลใน DLL | material/feature อาจไม่ทำงานใน clone | ตรวจ `GraphicsSettings`/shader ใน build |
| **U14** | เหตุผลที่ `FollowPair` รองรับเฉพาะ Dragon | `FollowPair.Init` cast เป็น `DragonBonesActor`; spine = no-op | ถ้าออกแบบให้ spine follow ต้องทำใหม่ | ตรวจ `FollowPair` source + ทดสอบ host ที่มี FollowPairs กับ spine |
| **U15** | การ reuse `m_SepSGRoot` ข้าม reload | สร้างเมื่อ null; `DestroySkeleton` destroy ทิ้ง → รอบใหม่สร้างใหม่ | ถ้าไม่ destroy อาจมีลูกค้าง | reload 2 รอบแล้วดู hierarchy |
| **U16** | `UpdateRectTransformSize` ถูกเรียกทุกเฟรมหรือเฉพาะตอน build/enable | เรียกจาก `SetupSpine` + `EnableUpdateRendererSize(true)` เท่านั้น | layout อาจไม่อัปเดตตาม animation | ดูขนาด RectTransform ระหว่าง animation |
| **U17** | semantics ของ `SkeletonGraphic.Update()` กับ `updateMode`/`freeze` | เป็น `Update(deltaTime)` ตาม `freeze`/`unscaledTime` | `ForceUpdate` advance ไม่ตรงที่คิด | ทดสอบ freeze + ForceUpdate |
| **U18** | ค่า `m_PMAVertexColor` ที่ผู้ใช้ตั้งจริงใน prefab | `UIModelShow.InitUISpinePara` ส่งค่าจาก serialized field | ภาพสีอาจต่าง | ดูค่าใน prefab/instance |
| **U19** | hierarchy/layer/sorting จริงใน prefab | พบ `UIDisciple > SpineActor > Root (SortingGroup disabled) > New Spine GameObject > Renderer*` | sorting/position ผิด | เปิด prefab ใน game_copy |
| **U20** | callback ยิงระหว่าง recycle หรือไม่ | `SkinReq.OnLoaded` เช็ค `m_IsLoading`; `ReplaceAttachmentReq` มี `Stop` | callback อาจแตะ object ที่ recycle แล้ว | instrument ตอน recycle ระหว่างโหลด |

---

## 2. วิธีทดสอบ runtime (ไม่แก้เกมติดตั้ง)

> ใช้สำเนา `game_copy` หรือโปรเจกต์ทดสอบแยก **ห้ามแก้ installation จริง**
> การทดสอบที่ต้องรันเกมจริงให้ทำบนสำเนาและเก็บผลลัพธ์ไว้ใน notes

### 2.1 อ่านค่าคอนฟิก (U01, U02, U18)

1. หา `AppDataModel` storage (player prefs/ไฟล์ config) ใน build
2. ค้น key `option.ActionSystem.UISpineActor.s_UISpineAlwaysMult`
3. บันทึกค่า + วันที่/บิลด์
4. สำหรับ `m_PMAVertexColor`/`m_CanvasGroupCompatible`/`m_TintBlack`:
   เปิด prefab ที่มี `UIModelShow` และอ่าน SerializedField

**ผลที่ต้องได้:** ค่า boolean/int + ที่มา (ไฟล์/บรรทัด)

### 2.2 ตรวจ material/shader (U03, U13)

1. เปิด scene/prefab ที่มี `GUIConfig`
2. อ่าน material ทั้ง 8 ช่อง → เปิด material → ดู shader name + keywords
3. บันทึก `GraphicsSettings` (Built-in/URP) และ quality tier

**ผลที่ต้องได้:** ตาราง 8 material → shader → feature (clip/tint/stencil)

### 2.3 ตรวจ separator/mount (U04, U09)

เพิ่ม instrumentation แบบอ่านอย่างเดียว (log) ในสำเนา:

```
ก่อน/หลัง SeparatorSkeleton:
  - separatorSlots.Count
  - SeparatorParts.Count
  - sibling index ของแต่ละ part
  - m_SeparatorBaseSortingOrder, m_SeparatorOrtingOrderIncreament
```

ทดสอบ 3 กรณี:
1. mount ปกติ (offset.z = 0)
2. mount offset.z > 0
3. mount ซ้ำหลัง unmount

**ผลที่ต้องได้:** ลำดับ sibling จริง + whether first call returns false

### 2.4 ตรวจ animation/cache (U06, U08, U11)

1. เปิด cache capture (`UIModelCacheItem`) ด้วย animationID ที่ไม่ใช่ 0
2. บันทึกภาพที่ได้ + `m_StateID`/`m_AnimationSpeed` หลัง build
3. เปรียบเทียบกับเส้นทาง `UIModelShow.SetTarget` (ที่เรียก `SetAnimationState` ก่อนโหลด)
4. ทดสอบ `SetAnimationSpeed(0.5)` แล้ววัด `timeScale` ของ main/sub

**ผลที่ต้องได้:** ค่า state จริง + ภาพ/เวลา animation

### 2.5 ตรวจ attachment (U05, U10, U12)

1. หา config ที่ `useSrcOffset=true`
2. เปลี่ยน slot ที่ attachment เป็น `MeshAttachment` (ถ้ามี) → ดู exception
3. ตั้ง skin name ผิด → ดู `SetSkin(null)`/NRE
4. สร้าง sub ที่ tintBlack ต่างกัน → `SetGray(true)` → ดู material ของแต่ละ SG

**ผลที่ต้องได้:** exception type/ข้อความ + material ที่ถูก assign

### 2.6 ตรวจ rect/force update (U16, U17)

1. เปิด `EnableUpdateRendererSize(true)` แล้วเล่น animation ที่ทำให้ bounds เปลี่ยน
2. วัด `RectTransform.sizeDelta`/`pivot` ทุกเฟรม
3. เรียก `ForceUpdate()` 2 ครั้งติด แล้ววัดเวลา animation

**ผลที่ต้องได้:** ค่า sizeDelta/pivot + delta time ของ animation

### 2.7 ตรวจ callback/recycle (U20)

1. เริ่มโหลด body/attachment แล้ว recycle ทันที
2. log ลำดับ callback (`OnBodyLoadFinish`, `OnAttachSkinFinish`, `OnAttachmentLoaded`)
3. ตรวจ exception จากการอ้าง object ที่ถูก destroy

**ผลที่ต้องได้:** ลำดับ callback + สถานะ `m_IsLoading`

---

## 3. สิ่งที่ยืนยันแล้ว (ไม่ใช่ Unknown)

เพื่อกันการกลับไปเดาซ้ำ:

| ข้อ | หลักฐาน |
|---|---|
| `DrawToBuffer` และ `SetShaderType` ว่าง (IL size 1) | CSV + source |
| `FlushSlashProperty` log error | source |
| `m_SortingGroup.enabled = false` ใน `CreateSpineComponent` | source |
| UI `StopAnimationState` ไม่ตั้ง `m_StateID`/`m_AnimationSpeed` | source (เทียบ base) |
| UI `SetFreezeAnimation` ไม่มีผล (`m_SkeletonAnimation` null) | source |
| `m_RaycastTarget` ไม่ถูกเขียน → false | grep ทั้งไฟล์ |
| `m_Mat`/`m_GrayMat` ถูกเขียนใน `CreateSkeletonGraphic` | source |
| PMA ถูกบังคับ false เมื่อ `!tintBlack && m_CanvasGroupCompatible` | source |
| `UpdateAnimationSpeed` ตั้ง sub `timeScale=1` | source |
| `SeparatorSkeleton` return true เมื่อ `separatorSlots.Count > 0` | source |
| `Mount` เรียก separator ก่อน async เสร็จ | source (base `LoadBody` ไม่ block) |
| `Skeleton.SetAttachment` throw ทั้ง slot/attachment ที่ไม่มี | `Spine/Skeleton.cs` (fork + stock) |
| `AnimationState.SetAnimation(int,string,bool)` ของ stock throw; fork มี `showErr` + คืน null | เทียบ 2 ไฟล์ |
| `UIModelShow.GenRenderTexure` เรียกทุกเฟรมถ้ามี RawImage | source |
| `FollowPair.Init` ใช้กับ `DragonBonesActor` เท่านั้น | source (สรุปจาก cast) |

---

## 4. Corrections ต่อเอกสารเดิม

รายการที่ต้องแก้จาก `REPORT.md` / สรุปก่อนหน้า / `IMPLEMENTATION_RESULT.md`:

| # | ข้อเดิม | ข้อที่ถูก (Observed) | ที่มา |
|---|---|---|---|
| R1 | `Skeleton.SetAttachment` "ไม่ทำอะไรเมื่อ slot ไม่มี" | **throw** `"Slot not found: ..."`; attachment ไม่มีก็ throw | `Spine/Skeleton.cs` |
| R2 | `StopAnimationState` ของ UI อธิบายแค่ stop | ไม่ตั้ง `m_StateID`/`m_AnimationSpeed`; ถ้า trackEntry null ไม่ reset `TimeScale`; sub ถูกตั้ง `timeScale=1` | `UISpineActor.cs:283-306` |
| R3 | `SetFreezeAnimation` ดูเหมือนใช้ได้ | UI **no-op** เพราะ base อ้าง `m_SkeletonAnimation` ที่ null | `SpineActor.cs:1034-1041` |
| R4 | `Mount` "แยก separator แล้ว fallback" | การเรียกครั้งแรกเกิดก่อน mount โหลดเสร็จ → มัก fallback; งานจริงเกิดใน base `DoMount` | `UISpineActor.cs:619-633`, `SpineActor.cs:1691-1721` |
| R5 | `raycastTarget` เป็นค่าที่ตั้งได้ | `m_RaycastTarget` ไม่ถูกเขียน → false เสมอ | `UISpineActor.cs:42,164` |
| R6 | `SetGray` ใช้ material ที่เลือกไว้ตั้งแต่สร้าง | จริง แต่ `m_Mat`/`m_GrayMat` ถูกเขียนทับโดย sub ตัวท้าย → อาจผิด | `UISpineActor.cs:119-147` |
| R7 | `EnablePmaVertexColor` ควบคุม PMA ตรง ๆ | ถูกบังคับ false ในเส้นทาง non-tint + canvasGroup | `UISpineActor.cs:136-141` |
| R8 | `UpdateAnimationSpeed` ใช้ค่า speed | UI ทิ้งค่า ตั้ง sub = 1 | `UISpineActor.cs:895-908` |
| R9 | `CreateFollow` error log ชี้ bone ที่หา | log ใช้ `attachBoneName` ไม่ใช่ `cmpRootBoneName` | `UISpineActor.cs:547` |
| R10 | `CreateSkeletonActor` อ่าน config ปลอดภัย | ไม่กัน null | `UISpineActor.cs:468-469` |
| R11 | `PlaySlotEffect` ใช้ canvas ได้เสมอ | ไม่กัน `canvas == null` | `UISpineActor.cs:425-426` |
| R12 | `DestroySkeleton` loop ที่ 2 มีผล | เป็น dead code ในเส้นทางปกติ (list ถูก clear แล้ว) | `UISpineActor.cs:87-110` |
| R13 | `SetUseClipping` มีผลทันที | ตั้ง settings เท่านั้น; mesh ต้อง rebuild | `UISpineActor.cs:265-281` |
| R14 | `OnAttachmentLoaded` เรียก `ForceUpdate` ครั้งเดียว | อยู่ในลูป `SkinNames` → อาจหลายครั้ง | `UISpineActor.cs:201-232` |
| R15 | `SetFlipY` เหมือน `SetFlipX` | ไม่ reset `m_SkeletonTransform.localScale` | `UISpineActor.cs:733-745` |
| R16 | `m_SeparatorBaseSortingOrder` เป็น 5 | field default 0; `RecycleSelf` เขียน 5 แล้วทับด้วย 0 | `SpineActor.cs:153,655-656` |
| R17 | `UpdateRectTranformSize` เป็นชื่อที่ถูก | เป็น typo ของเกม; ต้องคงชื่อถ้า drop-in | `UISpineActor.cs:70` |

---

## 5. Confidence / ระดับความมั่นใจ

| หมวด | ระดับ | เหตุผล |
|---|---|---|
| signatures/tokens/IL size | สูง | metadata + CSV |
| ลำดับการเรียกในคลาส | สูง | decompile ตรง |
| virtual dispatch | สูง | เทียบ base/override |
| พฤติกรรม material/animation | สูง | โค้ดตรง |
| ค่า config/material/shader จริง | ต่ำ | asset นอก DLL |
| พฤติกรรม runtime ที่ขึ้นกับเฟรม/AB | กลาง | อ่านโค้ดได้ แต่ไม่เห็นเวลา |
| เจตนาการออกแบบ (ทำไม) | ต่ำ | ต้องอนุมาน |

---

## 6. ลำดับการทดสอบที่แนะนำ (คุ้มที่สุดก่อน)

1. **U03/U13** (material/shader) — กระทบภาพมากที่สุด
2. **U04/U09** (separator/mount ordering) — กระทบ draw order
3. **U08** (cache animation) — กระทบภาพ cache โดยตรง
4. **U01** (alwaysMult) — กระทบ multi-renderer/atlas
5. **U05/U10/U12** (attachment/sub material) — edge cases
6. **U06/U11/U16/U17** — fine-tuning
7. **U02/U07/U14/U15/U18/U19/U20** — เก็บรายละเอียด

---

## 7. สิ่งที่ *ไม่ต้อง* ทดสอบซ้ำ

- จำนวนเมธอด 48 / token / IL size → CSV
- type identity / base class → metadata
- `DrawToBuffer`/`SetShaderType` ว่าง → IL size 1
- `SetFreezeAnimation` no-op → field null
- `raycastTarget` false → ไม่มีการเขียน
- `Skeleton.SetAttachment` throw → source
- `AnimationState.SetAnimation` ต่างกันระหว่าง fork/stock → source

> หากต้องการหลักฐานเพิ่ม ให้เพิ่มไฟล์ผลทดสอบใน `notes/uispineactor/` และอัปเดต
> `README.md` §2 + ตารางนี้
