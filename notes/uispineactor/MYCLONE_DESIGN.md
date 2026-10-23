# ขั้นที่ 2 — Design: SpineSystem สำหรับ `my_clone`

อ้างอิงหลักฐานทั้งหมดจาก `notes/uispineactor/REPORT.md` (E1/E2) — เอกสารนี้เป็น **การออกแบบใหม่**
ไม่ใช่การลอก implementation ของเกม และไม่ใช้ asset ของเกม

---

## 0. หลักการ 5 ข้อ

1. **แยก rendering ออกจาก logic** — animation/mount/flip/slot/gray อยู่ที่ base; UI กับ world ต่างกันแค่ "ตัว render"
2. **ไม่ทำ async AB loading** — clone รับ `SkeletonDataAsset` ตรง ๆ (Resources/Addressables เป็นเรื่องของผู้เรียก)
3. **ไม่เลียน quirk** — `UpdateAnimationState` ที่ตั้งให้แต่ sub-skeleton, `UpdateAnimationSpeed` ที่ทิ้งค่า `ts`,
   `SetShaderType`/`DrawToBuffer` ที่ว่างเปล่า → **ไม่ก๊อป** (ดู §3)
4. **SkeletonGraphic ต้องใช้ UI shader เท่านั้น** — ห้ามเอา `Spine/Skeleton*` (mesh) มาใส่ CanvasRenderer
5. **MVP ไล่ตามลำดับ 1→4** — 5 (bone follower/sub-skeleton) และ 6 (RT) ทำเป็น scaffold/ไม่ทำจนกว่าจะพิสูจน์ว่าจำเป็น

---

## 1. Responsibility split

| Layer | class (clone) | รับผิดชอบ | ไม่รับผิดชอบ |
|---|---|---|---|
| Contract / lifecycle | `SpineActorBase` (abstract MonoBehaviour) | build/release, `SkeletonDataAsset`, play/stop/progress/speed, loop, flip X/Y, slot attachment, gray state, mount lifecycle, color | การสร้าง component render, material matrix, sorting |
| UI render | `SpineUiActor : SpineActorBase` | สร้าง `SpineSkeletonGraphic` ใต้ RectTransform, material normal/gray, canvas group compat, separator slots, mount เป็น UI child, `MatchRectTransformWithBounds` | AB loading, HUD offset, soft mask, RT |
| World render | `SpineWorldActor : SpineActorBase` | สร้าง `SkeletonAnimation` + `MeshRenderer` + `SortingGroup`, MPB color, gray material (mesh shader), z-test/render queue | separator แบบ UI, Canvas |
| UI runtime patch | `SpineSkeletonGraphic : SkeletonGraphic` | เปิด `separatorSlotNames` (protected) ให้ตั้งได้ตอน runtime + `SetSeparatorSlotNames()` + `ApplySeparatorSlotsNow()` | ทุกอย่างอื่น |
| Creation | `SpineActorFactory` (static) | สร้าง GameObject + component + parent ให้ถูกชนิด (UI ต้องมี RectTransform) | load asset |
| Slot helper | (อยู่ใน base) | `SetSlotAttachment`, `ClearSlot`, `GetSlotAttachment`, `SetSkin` — 3-4 เมธอดสั้น ๆ ไม่คุ้มแยกคลาส | — |

> **ทำไมไม่แยก `SpineSlotController`**: Original มี `m_SlotMap` เพราะต้อง map slotID → attachment จาก config ที่โหลดมา
> แต่ clone รับชื่อ slot/attachment ตรง ๆ (เหมือน `UISpineActor.SetSlotAttachment` ที่เรียก `Skeleton.SetAttachment` ตรง)
> การแยกคลาสจะเป็นแค่ forwarder — ขัดหลัก lazy/minimal

### เทียบกับชื่อใน original (ตรวจแล้ว ไม่มีชื่อนี้ในเกม)

| ชื่อใน design | ที่มาจริงในเกม | หมายเหตุ |
|---|---|---|
| `SpineActorBase` | `ActionSystem.SpineActor` (base จริง) | รวมเฉพาะส่วนที่ UI/world ใช้ร่วม |
| `SpineUiActor` | `ActionSystem.UISpineActor` | ชื่อตั้งใหม่ให้ตรงหน้าที่ |
| `SpineWorldActor` | ไม่มีคลาสแยก — world คือ `SpineActor` เอง | แยกออกมาเพราะ clone ไม่มี inheritance 2 ชั้น |
| `SpineSkeletonGraphic` | `SkeletonGraphic` (fork ของเกม) | เกมแก้ในไฟล์ SkeletonGraphic โดยตรง; clone ใช้ subclass |

**หลักฐานว่าจำเป็นต้องมี subclass:** เกมมี `SetSerparatorSlotNames` (`SkeletonGraphic.cs:388`),
`UpdateRectTransformSize` (`:409`), `SeparatorParts` — stock 3.8 มี `separatorSlotNames` เป็น `protected`
(`Assets/Spine/.../SkeletonGraphic.cs:81`) และ `SeparatorParts` public read-only (`:87`) แต่ **ไม่มี setter ตอน runtime**
(`Initialize()` อ่านค่าครั้งเดียวที่ `:528-530`)

---

## 2. ลำดับการทำงาน (runtime flow ของ clone)

```
Factory.CreateUi(parent, data, settings)
  └─ AddComponent<SpineUiActor>()  (Awake ยังไม่สร้างอะไร)
SpineUiActor.Build()
  ├─ destroy old skeleton (ถ้ามี)
  ├─ SpineSkeletonGraphic.NewSkeletonGraphicGameObject(data, transform, material)
  │    ├─ material = gray ? grayMat : normalMat   (เลือกครั้งเดียวเหมือน original)
  │    ├─ allowMultipleCanvasRenderers / pmaVertexColors / tintBlack
  │    └─ Initialize(false)  → Skeleton + AnimationState พร้อม
  ├─ apply initial skin / flip
  ├─ SetGray(m_Gray)  (สลับ material)
  └─ Play(initialAnimation, loop)  (ถ้ามี)
```

`Release()` = `DestroySkeleton` + คืน material instance ที่สร้างเอง (ไม่ destroy asset ของผู้ใช้)

---

## 3. Original → clone: เก็บ / เปลี่ยน / ตัด

| Original behavior (evidence) | clone | เหตุผล |
|---|---|---|
| `CreateSpineComponent` → `CreateSkeletonGraphic` → ผูก `m_ISkeleton`/`m_IAnimationState` (REPORT §2.1) | `Build()` สร้าง SG แล้ว cache `Skeleton`/`AnimationState` | ตรงกัน |
| material 4 ชุด (tintBlack × canvasGroupCompatible) จาก `GUIConfig` (**ชื่อ shader = Unknown**) | 2 material ที่ผู้ใช้ส่งมา (`normalMaterial`, `grayMaterial`) + fallback shader ตามชนิด actor | ไม่มี `GUIConfig` ใน clone; ไม่เดา shader ของเกม |
| `SetGray` = สลับ material (+ sub-skeleton) | สลับ material ของ SG (และ mount actor) | ตรงกัน — ไม่ใช้ tint เพราะจะเพี้ยนกับ PMA |
| `SetSlotAttachment` = `Skeleton.SetAttachment` ตรง ๆ | เหมือนกัน + คืน `bool` + optional clear | ตรงกัน, เพิ่ม error visibility |
| `SetAnimationState` เลือกชื่อจาก `AnimationNameKey` + `RepeatCount` | รับ **ชื่อ animation ตรง ๆ** + `loop` flag | ไม่มี config table ใน clone |
| `UpdateAnimationState` (UI) ตั้งให้ sub-skeleton เท่านั้น และ return ถ้าไม่มี sub | ตั้งให้ **skeleton หลัก** เสมอ | quirk ของเกม (main ถูกตั้งโดย base `SetAnimation` อยู่แล้ว) — ไม่ก๊อป |
| `UpdateAnimationSpeed` (UI) ตั้ง `timeScale=1` ทิ้ง `ts` | `timeScale = speed` จริง | quirk — ไม่ก๊อป |
| `StopAnimationState` = `Update(end*progress)` แล้ว `timeScale=0` | `SetProgress(0..1)` + `Pause()` (timeScale=0) | แยก 2 เมธอดให้ชัด |
| `SetShaderType` (UI) = empty; (world) = ZTest/queue | UI: `NotSupportedException`-free no-op พร้อม comment; world: `SetZTest`/`SetRenderQueue` | ตามจริง |
| `DrawToBuffer` (UI) = empty | **ไม่ implement** (ดู §6) | RT ไม่ใช่ feature ของ UI spine |
| `FlushSlashProperty` (UI) = LogError | ไม่มี flash API | ไม่มีระบบ effect ใน clone |
| `Mount` ไม่ destroy mount actor, แค่ `OnRecycle` + `SetActive(false)` | คงพฤติกรรมนี้ (`UnMount` = ปิด, ไม่ destroy) + `DestroyMount()` เมื่อสั่งชัดเจน | ตรงกัน + มีทางคืน memory |
| mount ตำแหน่งตาม bone `mountHP` | `MountToBone(string bone)` ใช้ `BoneFollowerGraphic` (UI) / `BoneFollower` (world) | ง่ายกว่าและเป็น API ของ spine เอง |
| `SeparatorSkeleton` reparent `SeparatorParts` + sibling index | `SetSeparatorSlots(string[])` + `separatorParts` จัดการโดย SG; clone ไม่ย้าย sibling เอง | stock SG สร้าง part เป็นลูกของตัวเอง; การย้ายเป็นเรื่อง layout ของเกม |
| `CreateSubSkeleton` (UI) = แยก SG ต่อ attach skin | **defer** (ขั้น 5) | ต้องมีระบบ attach-skin ก่อน |
| `RecycleSelf` คืน AB refcount | `Release()` destroy component ที่สร้าง + clear cache; ไม่มี refcount | ไม่มี AB |

---

## 4. Shader / material strategy (สำคัญ)

### UI (`SkeletonGraphic`)
- ต้องเป็น `Spine/SkeletonGraphic` หรือ `Spine/SkeletonGraphic TintBlack` เท่านั้น (มี `_Stencil`, `unity_GUIZTestMode`, `UnityUI.cginc`)
- stock 3.8 **ไม่มี grayscale สำหรับ SkeletonGraphic** → clone เพิ่ม
  `Assets/Scripts/SpineSystem/Shaders/Spine-SkeletonGraphic-Grayscale.shader` (`Spine/SkeletonGraphic Grayscale`)
  — copy โครงจาก stock SkeletonGraphic แล้วแปลงสีใน fragment:
  `color.rgb = lerp(color.rgb, dot(color.rgb, luminance).xxx, _GrayAmount)`
  (ทำหลัง PMA/CanvasGroup แล้ว จึงไม่พัง alpha)
- `SpineUiActor` สร้าง material runtime:
  - `normalMaterial == null` → `new Material(Shader.Find("Spine/SkeletonGraphic"))`
  - `grayMaterial == null` → `new Material(normalMaterial) { shader = Shader.Find("Spine/SkeletonGraphic Grayscale") }`
  - ถ้า shader หาไม่เจอ → `Debug.LogError` + ใช้ normal (ไม่ throw) เพื่อไม่ให้เกมพังตอน build ที่ strip shader
- **ห้าม** ใช้ `Spine/Special/Skeleton Grayscale` กับ CanvasRenderer (เป็น mesh shader ไม่มี UI stencil/clip)

### World (`SkeletonAnimation`)
- `Spine/Skeleton` (lit/unlit ตามโปรเจกต์) + gray = `Spine/Special/Skeleton Grayscale`
- สี/alpha ใช้ `MaterialPropertyBlock` (`_Color`) ไม่แตะ material ที่ shared

### TintBlack
- clone รองรับเฉพาะ UI และเฉพาะเมื่อผู้ใช้ส่ง material ที่เป็น TintBlack มา + เปิด flag
- ไม่ทำ matrix 4 ชุดอัตโนมัติ (ต้องรู้ shader จริงของเกม — Unknown)

---

## 5. API ของ clone (สัญญาที่จะ implement)

```csharp
public abstract class SpineActorBase : MonoBehaviour
{
    public SkeletonDataAsset Data { get; }
    public bool IsBuilt { get; }
    public bool IsGray { get; }
    public bool FlipX { get; }
    public bool FlipY { get; }
    public Skeleton Skeleton { get; }
    public AnimationState AnimationState { get; }
    public SpineActorBase MountActor { get; }

    public abstract void Build();
    public abstract void Release();

    public void Play(string animation, bool loop = true, float speed = 1f);
    public void Play(string animation, bool loop, float speed, float startProgress);
    public void Stop();                     // timeScale = 0
    public void SetSpeed(float speed);
    public void SetProgress(float normalized);   // 0..1 ของ animation ปัจจุบัน
    public bool HasAnimation(string animation);
    public event Action<string> AnimationComplete;

    public void SetFlipX(bool flip);
    public void SetFlipY(bool flip);

    public bool SetSlotAttachment(string slot, string attachment);
    public bool ClearSlot(string slot);
    public string GetSlotAttachment(string slot);
    public bool SetSkin(string skin);

    public virtual void SetGray(bool gray);
    public abstract void SetColor(Color color);

    public SpineActorBase Mount(SkeletonDataAsset data, string skin = null);
    public void UnMount();                  // SetActive(false) — ไม่ destroy
    public void DestroyMount();
    public void MountToBone(string boneName);
}

public sealed class SpineUiActor : SpineActorBase
{
    public SpineSkeletonGraphic Graphic { get; }
    public void SetSeparatorSlots(params string[] slotNames);
    public void MatchRectTransformToBounds();
}

public sealed class SpineWorldActor : SpineActorBase
{
    public SkeletonAnimation Animation { get; }
    public void SetZTest(CompareFunction f);
    public void SetRenderQueue(int q);
}
```

---

## 6. MVP phases + acceptance

| ขั้น | สิ่งที่ทำ | acceptance |
|---|---|---|
| **1** | `SpineActorBase` + `SpineUiActor.Build/Release` + `Mount/UnMount` + factory + asmdef | สร้าง SG ได้, mount เป็นลูก, unmount ปิดแต่ไม่ destroy, release ไม่ leak component |
| **2** | `Play/Stop/SetSpeed/SetProgress/HasAnimation` + event complete | loop ไม่จบ, non-loop จบแล้ว event ครั้งเดียว, progress ตรง, speed มีผล |
| **3** | `SetFlipX/Y`, `SetSlotAttachment/ClearSlot/GetSlotAttachment/SetSkin` | scale -1 ที่ transform ของ skeleton, attachment เปลี่ยนจริงใน `Skeleton`, slot ไม่มี → false ไม่ throw |
| **4** | `SetGray` + shader ใหม่ + material fallback | สลับ material ไป-กลับได้, UI ยังถูก mask/clip, shader missing → error ชัดแต่ไม่ crash |
| **5** | `SetSeparatorSlots`, `MountToBone`, sub-skeleton | **scaffold เท่านั้น** — separator ตั้งได้, bone follower ทำงาน; sub-skeleton ยังไม่ทำ |
| **6** | `DrawToBuffer`/RT | **ไม่ทำ** — ต้องมีหลักฐานว่า UI ต้องใช้ (REPORT §5: UI ว่างเปล่า) |

---

## 7. Test strategy (ไม่ใช้ asset ของเกม)

- สร้าง Spine 3.8 JSON skeleton ขนาดเล็กใน test (bones 2, slot 2, skins 2, animations 2 ตัว: loop/non-loop)
  — ใช้ `"spine": "3.8.99"` เพราะโปรเจกต์มี guard ที่ปฏิเสธ `"3.8.75"` ตรง ๆ (ดู `notes/spine_setup.md`)
- atlas text 1 page + `Texture2D` สีขาว 64×64 ที่สร้างในโค้ด
- `SpineAtlasAsset.CreateRuntimeInstance(atlasText, textures, shader, true)`
- `SkeletonDataAsset.CreateRuntimeInstance(jsonAsset, atlas, true, 0.01f)`
- รันเป็น EditMode test (สร้าง GameObject + `Canvas` ในซีนชั่วคราว) แล้ว `DestroyImmediate`
- เคส: build/release, play loop/non-loop, progress, flip, slot, skin, gray, mount/unmount, separator, world actor

> หมายเหตุ: `SkeletonGraphic` ต้องอยู่ใน `Canvas` จึงจะ rebuild ได้ — test ต้องสร้าง `Canvas` + `CanvasRenderer` ให้ครบ
> และ atlas text ต้องมี **บรรทัดว่างนำหน้า** เพราะ `SpineAtlasAsset.CreateRuntimeInstance` หา page name
> จาก "บรรทัดถัดไปของบรรทัดว่าง"

---

## 8. สิ่งที่ยัง Unknown และไม่เติมเอง

1. ชื่อ shader/material จริงของเกม (`GUIConfig.uiSpineMat*`) — ใช้ของผู้ใช้แทน
2. `m_SeparatorBaseSortingOrder` ค่าจริง — clone ให้ผู้เรียกระบุ
3. render pipeline ของโปรเจกต์เดิม — clone ใช้ Built-in UI shader (ตรงกับ stock 3.8 ที่มี)
4. sub-skeleton/attach-skin workflow ของเกม — defer ขั้น 5
5. เหตุผลที่ UI `DrawToBuffer` ว่าง — สันนิษฐานว่า UI ใช้ CanvasRenderer ไม่ต้องวาดลง RT (Derived จาก REPORT §4)

---

## 9. โครงไฟล์ที่จะสร้าง

```
my_clone/Assets/Scripts/SpineSystem/
  MyClone.SpineSystem.asmdef          (references: spine-unity)
  SpineActorBase.cs
  SpineUiActor.cs
  SpineWorldActor.cs
  SpineSkeletonGraphic.cs
  SpineActorFactory.cs
  Shaders/Spine-SkeletonGraphic-Grayscale.shader
my_clone/Assets/Tests/SpineSystem/
  MyClone.SpineSystem.Tests.asmdef    (Editor only, NUnit)
  TestSkeletonFactory.cs
  SpineUiActorTests.cs
  SpineWorldActorTests.cs
```
