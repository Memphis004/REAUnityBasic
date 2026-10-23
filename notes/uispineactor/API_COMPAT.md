# API_COMPAT — `ActionSystem.UISpineActor`

เอกสารนี้ตอบคำถาม: **"clone ต้องมี surface อะไร และพฤติกรรมไหนถือว่าเข้ากันได้"**
พร้อมเทียบสถานะ MVP ปัจจุบันของ `my_clone` (ณ เอกสารนี้) โดยไม่แก้โค้ดในเฟสนี้

นิยามความเข้ากันได้ 2 ระดับ:

| ระดับ | ความหมาย | ใช้เมื่อ |
|---|---|---|
| **A. Semantic compatibility** | ทำงานเหมือนในเชิงผลลัพธ์/ลำดับที่สังเกตได้ โดยใช้ type/API ของ clone เอง | แนะนำสำหรับ `my_clone` |
| **B. Drop-in signature compatibility** | ชื่อ type/method/พารามิเตอร์ตรงกับเกม เพื่อให้ host เดิมเรียกได้ | ต้องใช้เมื่อแทนที่แบบ binary/compile กับโค้ดเกม |

> เอกสารนี้ยึด **A** เป็นหลัก และระบุ surface ของ B ไว้เผื่ออนาคต
> (ไม่คัดลอก implementation ของเกม)

---

## 1. Type identity ที่ต้องคงไว้ (สำหรับระดับ B)

```csharp
namespace ActionSystem
{
    public class UISpineActor : SpineActor
    {
        public static bool s_UISpineAlwaysMult { get; set; }
        public override SkeletonGraphic skeletonGraphic { get; }
        // ... 48 เมธอดตาม BEHAVIOR.md
    }
}
```

- `UISpineActor` ต้องสืบทอด `SpineActor` (ซึ่ง implement `IActor`)
- `skeletonGraphic` ต้องเป็น `SkeletonGraphic` (ไม่ใช่ `SkeletonAnimation`)
- ชื่อฟิลด์ protected ที่ base/host แตะต้องคงไว้ถ้าจะ compile ร่วม:
  `m_SkeletonGraphic`, `m_SepSGRoot`, `m_subSkeletonGraphics`, `m_useClipping`,
  `m_CanvasGroupCompatible`, `m_PMAVertexColor`, `m_TintBlack`
- ชื่อที่พิมพ์ผิดต้องคงถ้าต้องการ signature เดิม: `UpdateRectTranformSize`, `SperatorSlot`

---

## 2. Public/protected surface ของ `UISpineActor`

### 2.1 Public

| signature | ใช้โดย host |
|---|---|
| `static bool s_UISpineAlwaysMult { get; set; }` | AppDataModel |
| `override SkeletonGraphic skeletonGraphic { get; }` | ภายใน/cache |
| `void EnableUpdateRendererSize(bool)` | `UIModelShow` |
| `void ForceUpdate()` | `UIModelCacheItem` |
| `void SetUseClipping(bool)` | `UIModelCacheItem` |
| `override void StopAnimationState(int, float = 0)` | `IActor` |
| `void EnablePmaVertexColor(bool)` | `UIModelShow` |
| `void EnableCanvasGroupCompatible(bool)` | `UIModelShow` |
| `void EnableTintBlack(bool)` | `UIModelShow` |
| `void SetSlotAttachment(string, string)` | `UIModelShow` |
| `override void SetUpdateMode(int)` | `IActor` / mount |
| `override void UpdateColor_Imp(Color)` | base/owner |
| `override void SetGray(bool)` | `IActor` |
| `override void DrawToBuffer(CommandBuffer)` | `IActor` (no-op) |
| `override void SetShaderType(int)` | `IActor` (no-op) |
| `override void BuildCmpBody(FollowSpineActor, int = 0)` | base |
| `override void FlushSlashProperty()` | base (log only) |
| `override bool SeparatorSkeleton(string[], int, int)` | base/mount |
| `override void Mount(int, int[], string, float, Vector3, Action)` | `IActor` |
| `override void UnMount()` | `IActor` |
| `override void SetFlipX(bool)` / `SetFlipY(bool)` | `IActor` / base |
| `override void UpdateAnimationState*` (protected) | base hooks |

### 2.2 Protected / internal (สำหรับ subclass/adapter)

| signature | หมายเหตุ |
|---|---|
| `internal SkeletonGraphic CreateSkeletonGraphic(SkeletonDataAsset, bool gray, bool tintBlack = false)` | ใช้สร้าง main + sub |
| `override void CreateSpineComponent(SkeletonDataAsset)` | hook |
| `override void DestroySkeleton()` | hook |
| `override void SetupSpine()` | hook |
| `override void OnAttachmentLoaded(ReplaceAttachmentReq)` | callback |
| `override void FlushHudOffset()` | ×125 |
| `override void DoUpdateColor()` | |
| `override Transform GetBoneFollowerTransform(string)` | |
| `override void PlaySlotEffect(SlotEffectInfo)` | |
| `override void RecycleSelf()` | |
| `override FollowSpineActor CreateSkeletonActor(...)` | |
| `protected void SperatorSlot(Transform, string)` | |
| `override void CreateFollow(FollowSpineActor)` | |
| `override void CreateMountActor()` | |
| `override void UpdateMount()` | |
| `override void CreateSubSkeleton(List<SkinReq>)` | |
| `private void FlushTintBlack(bool)` | |
| `protected SkeletonGraphic CreateSubSkeletonGraphic(SkinReq, Dictionary<string,int>, List<IndexTransform>, out bool)` | |
| `private void SeparatorSkeletoGraphics(...)` | |
| `protected void UpdateRectTranformSize()` | |
| `private void InitWithUISpineActor(UISpineActor)` | |

### 2.3 `IActor` ที่ host ใช้ (ต้องทำงานถูกแม้ไม่ override)

`ModelRoot`, `SetSortingLayer/Order`, `SetScale`, `SetFlipX/Y`, `SetOffset`,
`RayHit`, `EnableRayHit`, `RayHitPerspective`, `SetAnimationState`,
`StopAnimationState`, `SetAnimationStateWithProgress`, `SetAnimationSpeed`,
`GetSortID`, `GUILoadBody`, `LoadBody` (2 overload), `LoadSlot`,
`SetCullableManager`, `HideSlot`, `OnRespawn`, `OnRecycle`, `PlayEffect` (2),
`RotationEffect`, `SetColor`, `GetColor`, `FadeToColor`, `DOLocalMove` (2),
`StopLocalMove`, `GetRootTransform`, `ShowShadow`, `Mount`, `UnMount`,
`SetSlotInheritColor`, `Twinkle`, `ChangeSlotDisplay`, `AddSkeletonSlot`,
`ChangeFollowActorSlotDisplay`, `GetScale`, `GetHudOffset`,
`GetBottomHudOffset`, `GetFlipX`, `SetShaderType`, `SetShaderRenderQueue`,
`PlayEffectOnActor`, `RemoveEffectOnActor`, `GetSlotTransform`,
`SetAnimation(bool)`, `CheckCulling`, `ChangeSlotImage`, `DrawToBuffer`,
`SetUpdateMode`, `SetGray`, `SetFreezeAnimation`

> UI override เพียงบางส่วน; ที่เหลือใช้ base ซึ่งบางตัวเป็น no-op โดยเจตนา
> (`CheckCulling`, `SetAnimation(bool)`, `SetShaderRenderQueue` ยังทำงานกับ mesh)

---

## 3. Checklist ความเข้ากันได้ (semantic)

สถานะของ `my_clone` อ้างจาก `IMPLEMENTATION_RESULT.md` + โค้ดปัจจุบัน
(`my_clone/Assets/Scripts/SpineSystem`)

สัญลักษณ์: ✅ ทำแล้ว · ◐ บางส่วน · ❌ ยังไม่มี · ⚠ ต่างโดยเจตนา · — ไม่เกี่ยวกับ UI

### P0 — ต้องมีก่อนถือว่าใช้แทนได้

| ID | Requirement | หลักฐาน | สถานะ clone |
|---|---|---|---|
| C01 | สร้าง `SkeletonGraphic` ใต้ root + `Initialize` | §9/§10 | ✅ |
| C02 | destroy/สร้างใหม่ได้ (idempotent) | §10 | ✅ |
| C03 | เลือก material normal/gray | §9/§22 | ◐ (clone สร้าง material instance เอง; ไม่ใช้ `GUIConfig`) |
| C04 | `SetGray` สลับ material main + sub | §22 | ◐ (main ✅; sub ❌ เพราะยังไม่มี sub) |
| C05 | animation main play/stop/progress/speed | base | ✅ (แต่ semantics ต่าง — ดู C20–C22) |
| C06 | `SetSlotAttachment` ผ่าน `Skeleton.SetAttachment` | §17 | ⚠ (clone validate + คืน bool + warning; เกม throw) |
| C07 | `SetFlipX/Y` | §39/§40 | ◐ (ยังไม่มี `SepSGRoot` path) |
| C08 | `DrawToBuffer`/`SetShaderType` no-op | §25/§26 | ✅ (ไม่ implement = no-op) |
| C09 | `FlushSlashProperty` แจ้งไม่รองรับ | §33 | ❌ (clone ไม่มี flash/twinkle) |
| C10 | `RecycleSelf` เคลียร์ state + destroy renderer | §27 | ✅ (แต่ยังไม่มี sub/follow) |
| C11 | mount: สร้าง child, unmount = deactivate | §34–§38 | ◐ (มี mount/unmount/destroy; ตำแหน่งใช้ BoneFollower ไม่ใช่ PPU) |
| C12 | `SetUseClipping` | §15 | ❌ (มีเฉพาะ field ตอน build; ไม่มี runtime setter) |
| C13 | `SetUpdateMode` | §18 | ❌ (clone ไม่มี update mode) |
| C14 | `ForceUpdate` | §14 | ❌ |

### P1 — ต้องมีเพื่อให้ฟีเจอร์ UI ครบ

| ID | Requirement | หลักฐาน | สถานะ clone |
|---|---|---|---|
| C15 | material 4 คู่ (tintBlack × canvasGroup) | §9 | ◐ (มี tintBlack/canvasGroup + shader; ไม่มี 8 ช่อง config) |
| C16 | PMA ถูกบังคับ false เมื่อ non-tint + canvasGroup | §9 | ✅ |
| C17 | `allowMultipleCanvasRenderers` ตาม alwaysMult/atlas/material | §9 | ◐ (alwaysMult + separator + atlas rule มี) |
| C18 | tintBlack + canvasGroupTintBlack + TexCoord1/2 | §9/§44 | ◐ (มี tintBlack + shader; gray+tintBlack ยังเตือน) |
| C19 | rect transform auto-size | §4/§13 | ◐ (`MatchRectTransformToBounds`; ไม่มี flag auto ทุก build) |
| C20 | `StopAnimationState` หยุด main | §16 | ✅ (แต่ clone หยุดด้วย `AnimationState.TimeScale=0`, ไม่ update ตาม progress) |
| C21 | sub-skeleton sync animation | §45–§47 | ❌ |
| C22 | sub-skeleton ทั้งชุด (`SepSGRoot`, sort, BoneFollower) | §41–§43 | ❌ |
| C23 | attach skin (`SkinReq`) + follow actor | §12/§28–§32 | ❌ |
| C24 | separator parts reparent + sibling index | §35 | ◐ (scaffold `SetSeparatorSlots`; ยังไม่ reparent/sibling) |
| C25 | `SperatorSlot` สำหรับ follow actor | §31 | ❌ |
| C26 | mount ตำแหน่งตาม bone × `referencePixelsPerUnit` | §38 | ❌ (ใช้ BoneFollowerGraphic) |
| C27 | mount separator + fallback sibling | §36 | ❌ |
| C28 | follow actor animation forwarding | base | ❌ |
| C29 | `OnAttachmentLoaded` (replace attachment) | §12 | ❌ |
| C30 | effect ตาม bone (`PlayUIEffectEx` + BoneFollower) | §23/§24 | ❌ |
| C31 | HUD offset ×125 | §19 | ❌ |
| C32 | `ChangeSlotImage` / image display | base | ❌ |

### P2 — quirk ที่ต้องเลือกว่าจะเลียนแบบหรือไม่

| ID | Quirk | หลักฐาน | คำแนะนำ |
|---|---|---|---|
| Q01 | `raycastTarget` เป็น false เสมอ | §9 | ทำตามได้ (clone มี field; default false) |
| Q02 | `StopAnimationState` ไม่บันทึก `m_StateID`/`m_AnimationSpeed` | §16 | ⚠ อย่าเลียนแบบถ้าไม่จำเป็น (เป็น bug) |
| Q03 | `UpdateAnimationSpeed` ทิ้งค่า speed ของ sub | §47 | ⚠ clone ใช้ค่าจริง (ดีกว่า) — documented |
| Q04 | `SetFreezeAnimation` ไม่มีผลกับ UI | §4.5 | clone ควรทำให้ได้ถ้าต้องการ freeze |
| Q05 | `m_Mat`/`m_GrayMat` ถูกเขียนทับโดย sub ตัวท้าย | §4.3 | อย่าเลียนแบบ; เก็บ material ต่อ SG |
| Q06 | `Mount` แยก separator ก่อนโหลดเสร็จ | §4.6 | อย่าเลียนแบบ; ทำให้ synchronous/queue |
| Q07 | `SeparatorSkeleton` return true ถ้ามี slots เดิม | §4.7 | อย่าเลียนแบบ; เทียบชื่อ/clear ก่อน |
| Q08 | `OnAttachmentLoaded` ForceUpdate ในลูป | §4.8 | หลีกเลี่ยง; เรียกครั้งเดียวหลังจบ |
| Q09 | `SetFlipY` ไม่ reset scale | §4.11 | แก้ให้สมมาตร |
| Q10 | error log ชื่อ bone ผิด | §4.12 | แก้ข้อความ |
| Q11 | `PlaySlotEffect`/`CreateSkeletonActor` ไม่กัน null | §4.13 | เพิ่ม guard |
| Q12 | PMA forced false (non-tint + canvasGroup) | §4.4 | ทำตามถ้าต้องการภาพตรง |
| Q13 | `FlushHudOffset` ×125 | §19 | ทำตามถ้าต้องการ HUD ตรง |
| Q14 | `SortingGroup.enabled = false` | §10 | ทำตาม |

---

## 4. Mapping surface เกม → clone ปัจจุบัน

| เกม | clone (`my_clone/Assets/Scripts/SpineSystem`) | หมายเหตุ |
|---|---|---|
| `UISpineActor` | `SpineUiActor` | คนละชื่อ; semantic |
| `SpineActor` | `SpineActorBase` + `SpineWorldActor` | แยก world/UI |
| `CreateSkeletonGraphic` | `SpineUiActor.CreateRenderer` + `ResolveNormal/GrayMaterial` | |
| `DestroySkeleton` | `DestroyRenderer` | |
| `SetGray` | `SetGray` | |
| `SetSlotAttachment` | `SetSlotAttachment` (คืน bool) | ⚠ strict กว่า |
| `StopAnimationState` | `Stop` | ⚠ semantics ต่าง |
| `SetAnimationState` | `Play(name, loop, speed)` | clone ใช้ชื่อ animation ตรง ๆ |
| `SetAnimationStateWithProgress` | `SetProgress(normalized)` | |
| `SetAnimationSpeed` | `SetSpeed` | clone ใช้ค่าจริง |
| `EnableUpdateRendererSize` | `MatchRectTransformToBounds()` (manual) | ยังไม่มี flag |
| `SetUseClipping` | field `useClipping` ตอน build | ยังไม่มี runtime setter |
| `SetUpdateMode` | — | ยังไม่มี |
| `ForceUpdate` | — | ยังไม่มี |
| `s_UISpineAlwaysMult` | `alwaysMultipleCanvasRenderers` (serialized) | ยังไม่มี static config |
| `s_UISpineLoadPriority` | — | ไม่มี async loader |
| `SeparatorSkeleton` | `SetSeparatorSlots` + `SpineSkeletonGraphic.SetSeparatorSlotNames` | scaffold |
| `Mount` (int id, slots, hp, scale, offset, cb) | `Mount(SkeletonDataAsset, skin)` | คนละ data flow |
| `UnMount` | `UnMount` | ✅ |
| `CreateMountActor` | `CreateMountActor` | ✅ (RectTransform) |
| `UpdateMount` | `AttachMountToBone` (BoneFollower) | คนละวิธี |
| `CreateSubSkeleton` | — | ยังไม่มี |
| `CreateSkeletonActor/BuildCmpBody/CreateFollow` | — | ยังไม่มี |
| `PlaySlotEffect` | — | ยังไม่มี |
| `SetMountSeparatorSlotNames` | — | ยังไม่มี |
| `FlushHudOffset` | — | ยังไม่มี |

---

## 5. สิ่งที่ clone ทำ "ดีกว่า" และควรคงไว้ (ไม่ใช่ incompatibility)

1. `Play` ใช้ `speed` จริง (เกมทิ้งค่าใน sub hook)
2. `SetSlotAttachment` ตรวจชื่อและคืน `false` + warning แทนการ throw
3. `SetSpeed` ใช้ค่าจริง
4. material เป็น instance ต่อ actor (ไม่แชร์ asset) → ปลอดภัยกว่า
5. `Stop`/`Pause`/`Resume`/`Tick` แยกชัดเจน
6. `HasAnimation` ก่อน `SetAnimation` → กัน exception ของ stock 3.8

> แต่ถ้าต้องการ "ภาพตรงเกม" ต้องเพิ่ม compatibility mode ที่เลือกพฤติกรรม P2
> (เช่น `Stop` ไม่บันทึก state, sub speed=1, PMA forced)

---

## 6. Acceptance criteria (ก่อนถือว่า compatible)

### 6.1 Must pass (P0)

- [ ] สร้าง → แสดง → recycle → สร้างซ้ำ ได้โดยไม่มี object/material รั่ว
- [ ] `SetGray` สลับได้ทั้ง main และ sub
- [ ] `SetSlotAttachment` เปลี่ยน attachment ที่มีอยู่ได้
- [ ] `SetFlipX/Y` ไม่สะสม scale
- [ ] mount → unmount → mount ซ้ำ ได้; unmount ไม่ destroy
- [ ] `DrawToBuffer`/`SetShaderType` ไม่ throw และไม่ทำอะไร
- [ ] ไม่มี NRE เมื่อเรียกก่อน build (SetSlotAttachment/SetUseClipping/SetUpdateMode/ForceUpdate)

### 6.2 Should pass (P1)

- [ ] attach skin ที่มี `cmpRootBone` → follow actor แสดงและตาม bone
- [ ] attach skin ที่ไม่มี `cmpRootBone` → sub-skeleton + separator ตาม slot order
- [ ] separator slots: part ถูก reparent + sibling index ตาม config
- [ ] mount ตาม bone ด้วย `referencePixelsPerUnit`
- [ ] `ForceUpdate` ใช้ก่อน cache capture แล้วได้ภาพนิ่งที่ถูก
- [ ] tintBlack + canvasGroup 4 combininations แสดงถูก
- [ ] rect auto-size ตาม mesh

### 6.3 Known acceptable differences

- material instance vs shared `GUIConfig`
- synchronous mount vs async
- `SetSlotAttachment` validation
- speed จริง vs quirk
- ไม่มี flash/shadow/light/RT (นอกขอบเขต UI)

---

## 7. ข้อสรุปสำหรับการตัดสินใจ

| คำถาม | คำตอบ |
|---|---|
| ต้อง implement ทั้ง 48 เมธอดไหม | **ไม่** — หลายตัวเป็น hook ว่าง/accessor; ขั้นต่ำ ~20 เมธอดตาม P0/P1 |
| ต้องมี `SpineActor` base ทั้ง 159 เมธอดไหม | ไม่ — เฉพาะที่ UI/host ใช้; แต่ถ้าต้อง compile ร่วมต้องมี surface ของ `IActor` ครบ |
| ต้องมี `SkeletonGraphic` fork ไหม | ต้องมี 2 API (`SetSerparatorSlotNames`, `UpdateRectTransformSize`) หรือ shim |
| clone ปัจจุบันถือว่า compatible ไหม | **P0 ส่วนใหญ่, P1 บางส่วน** — ยังขาด sub-skeleton/follow/attach/ForceUpdate/mount PPU |
| ต้องเปลี่ยนชื่อเป็น `UISpineActor` ไหม | ไม่ ถ้าใช้ semantic compatibility; ต้องเปลี่ยนถ้าจะ drop-in กับ host เดิม |

---

## 8. การเชื่อมกับเอกสารอื่น

- signature/พฤติกรรมเต็ม → `BEHAVIOR.md`
- ลำดับเรียก → `CALLGRAPH.md`
- ของที่ต้องมี → `DEPENDENCIES.md`
- ลำดับชีวิต/cleanup → `LIFECYCLE.md`
- สิ่งที่ยังไม่รู้/วิธีทดสอบ → `UNKNOWNS.md`
- ผล MVP และข้อจำกัดเดิม → `IMPLEMENTATION_RESULT.md`
