# หน้าต่าง Disciple (UIDiscipleRoleInfo2Win) — วิเคราะห์ + สร้างใหม่ใน my_clone

> ขอบเขต: หน้าต่างนี้หน้าต่างเดียว รอบนี้ไม่ทำหน้าต่างอื่น (ชื่อหน้าต่างที่ใช้โครงเดียวกันอยู่ในข้อ 9)
> เกมอ่านอย่างเดียว: `C:\Workspace\REA\game_copy` ไม่มีการแก้ไฟล์เกม ไม่มี DLL/สคริปต์/`MonoBehaviour` ของเกมถูกใช้ใน my_clone
> ทุกข้อความแยกสถานะ **observed** (อ่านได้ตรงจากไฟล์/ข้อมูล), **inferred** (อนุมานจากหลักฐาน), **unknown** (ยังไม่มีหลักฐาน)

---

## 0. สรุปสั้น

| ขั้น | ผล |
|---|---|
| 1. หา prefab ด้วย UnityPy | เจอที่ `game_copy/Windows/ui/windows/disciple/uidiscipleroleinfo2win.ab` (19,018 bytes) — ยืนยันด้วย `gui.uiwindowconfig_auto.txt` ที่ระบุ `ab='ui/windows/disciple/uidiscipleroleinfo2win.ab'` และ `canvas=3` |
| 2. dump เป็น JSON | `extracted/dump_uidisciple_roleinfo2.py` → `extracted/uidiscipleroleinfo2.json` (115 GameObject, 79 Image, 8 Text, 15 Button, 44 bound widget) แล้วแปลงเป็น `my_clone/Assets/_Generated/Data/disciple_window.json` ด้วย `extracted/build_window_json.py` |
| 3. วิเคราะห์ framework | binding เป็น **index** ไม่ใช่ชื่อ; `#`-prefix = ชื่อ child ที่ถูก export ไปผูกกับ Lua 1:1 (44 ตัว); เลเยอร์มาจาก `canvas=3` → `UICanvas[3]` = `Canvas_BottomFirstLayer` (ข้อ 4) |
| 4. Editor builder | `my_clone/Assets/Editor/UIWindowBuilder.cs` → `Assets/_Generated/UI/DiscipleWindow.prefab` (115 GameObject + SpineUiActor + placeholder) |
| 5. ระบบใหม่ | `Assets/Scripts/UI/{WindowBase,WindowManager,DiscipleWindow}.cs` |
| 6. scene | `Assets/_Generated/UIDemo.unity` — กด Play แล้วเห็นหน้าต่างทันที (ตรวจด้วย Play mode จริง ข้อ 6) |
| 7. เอกสาร | ไฟล์นี้ |

คำสั่งรัน: Unity 2021.3.45f2 → เปิด `Assets/_Generated/UIDemo.unity` → Play
เมนู build: `Tools/UI/Build Disciple Window (Placeholder)` / `Tools/UI/Build Disciple Window (Local Reference Sprites)` / batch `MyClone.EditorTools.UIWindowBuilder.BuildBatch`

---

## 1. ค้นหา prefab (observed)

สแกน/ตรวจตามลำดับนี้ ไม่มีการเดา path:

| ที่ค้น | ผล |
|---|---|
| `game_copy/Windows/ui/windows/disciple/**` | **พบ** `uidiscipleroleinfo2win.ab` + `*_atlas_pak.ab` (ใช้หา sprite) |
| `game_copy/Windows/ui/windows/disciple/sharedtextures/{uidisciplejobicons,uidisciplemainicons}.ab` | ใช้เป็นแหล่ง sprite เพิ่มเติม (อ่านเฉพาะชื่อ) |
| `game_copy/Immortal Clan Grandmastery_Data/StreamingAssets/Windows/ui/windows/disciple/` | มีแต่ `sharedtextures/` — **ไม่มี** bundle ของหน้าต่างนี้ |
| `game_copy/Immortal Clan Grandmastery_Data/StreamingAssets/Windows/preload/ui/guisettings.ab` | พบ GUISettings (ใช้ยืนยัน Canvas/Camera/UICanvas index) |
| `extracted/cab_index_ui.json` (index ของ bundle ทั้งหมด) | ใช้ยืนยันชื่อ bundle/asset ไม่มีรายการซ้ำ |

ข้อมูลใน bundle: prefab รากชื่อ `UIDiscipleRoleInfo2Win`, มี `CSGUIWindowLua` 1 ตัว, `UIModelShow` ที่ `#discipleModelRoot`, `CSGUIImage` หลายตัว, `CSGUICommonLayoutGroup` 2 ตัว, `CSGUIBaseItem` ต่อ item, `CSGUIEffect` 1 ตัว, `Outline` 3 ตัว, `RectMask2D` 1 ตัว

> หมายเหตุ: ใน prefab มี child ชื่อซ้ำกัน 2 ตัวคือ `Image` ใต้ `#root` (sibling 0 ว่าง ๆ กับ sibling 2 ที่มีชื่อ/อาชีพ/ปุ่มเปลี่ยนชื่ออยู่ข้างใน) ตัว dump จึง key ด้วย `path_id` และ JSON ที่ builder ใช้ตั้งชื่อซ้ำเป็น `Image#0`, `Image#2`

---

## 2. Canvas / Camera / เลเยอร์ (observed จาก GUISettings)

`GUISettings.UICanvas` (18 ช่อง) resolve ชื่อได้: `[0] Canvas_Scene, [1] Canvas_SceneTips, [2] Canvas_UIActor, [3] Canvas_BottomFirstLayer, [4] Canvas_BottomSecondLayer, [5] Canvas_BottomThirdLayer, [6] Canvas_WindowFirstLayer, [7] Canvas_WindowSecondLayer, [8] Canvas_WindowThirdLayer, [9] Canvas_DialogLayer, [10] Canvas_ModelTipOnUITopLayer, [11] Canvas_topMaskTopLayer, [12] Canvas_SystemTipMessage, [13] Canvas_SystemTopMessage, [14] OverLayerContain, [15] SuperOverLayerContain, [16] CustomCanvas, [17] Canvas_DialogLayer_Fight`

`Canvas_BottomFirstLayer` (ค่าที่ใช้ใน my_clone):

| ค่า | ต้นฉบับ | ใน my_clone |
|---|---|---|
| Render Mode | ScreenSpaceCamera (`m_RenderMode=1`), camera = `UICamera` | ScreenSpaceCamera + `UICamera` |
| Plane Distance | 100 | 100 |
| Sorting Order | 1000 | 1000 |
| Sorting Layer ID | 46863019 | default (scene ใหม่ไม่มี SortingLayer ของเกม) |
| Pixel Perfect | false | false |
| Additional Shader Channels | 27 (`TexCoord1|TexCoord2|Normal|Tangent`) | 27 |
| CanvasScaler | ScaleWithScreenSize, ref **1334×750**, MatchWidthOrHeight, match **0** (width), refPPU 100, dynamicPPU 1 | เหมือนกันทั้งหมด |
| Camera | orthographic, size **5**, near 0.3, far 1000, depth 15, clearFlags 3 (Depth), cullingMask 232 (layer 3,5,6,7) | ortho 5, near 0.3, far 1000, depth 15, **SolidColor** (ฉากเดโมไม่มีฉาก 3D ด้านหลัง), cullingMask = layer 5 อย่างเดียว |
| ชื่อ container | `GUI_ROOT/BottomUIContain/Canvas_BottomFirstLayer` | เหมือนกัน (มี `GUI_ROOT` + `BottomUIContain` เป็น parent จริง) |

**เรื่อง x = −10000**: `CSGUIWindowBase.OUT_OF_VIEW_3D = new Vector3(-10000, 0, 0)` และ `Canvas_Scene` เป็น **WorldSpace** ที่ `anchoredPosition.x = -10000`, `localScale = 0.013333` (=1/75) — เป็นวิธีซ่อน canvas นอกจอ ไม่เกี่ยวกับหน้าต่างนี้ (หน้าต่างนี้อยู่บน canvas ปกติที่กึ่งกลางจอ) → **observed**

---

## 3. โครงสร้าง prefab (observed)

115 GameObject, ชั้นลึกสุด 8, root เป็น RectTransform แบบ stretch (anchor 0..1, sizeDelta 0) และ `#root` เป็น 100×100 กึ่งกลาง (pivot 0.5) — child ทั้งหมดอ้างตำแหน่งจาก `#root` ตัวนี้

```
UIDiscipleRoleInfo2Win            (stretch, CSGUIWindowLua)
└─ #root                          100×100 @ (0,0)   [CanvasGroup]
   ├─ Image                      300×44  @ (-237, 235)   (แถบพื้นหลังชื่อ — sibling 0)
   ├─ mask                       522×532 @ (-245, -76.8) [RectMask2D]
   │  ├─ #discipleModelRoot      477×395 @ (0, 55)       [UIModelShow]
   │  └─ #rolespbtn              300×300 @ (0, 0)        [Button, Image]
   ├─ Image#2                    300×44  @ (-237, 235)
   │  ├─ #discipleNameText       200×40  [Text 24 MiddleCenter]
   │  ├─ #discipleJobBtn          44×44  @ (-110, 0)     [Button]
   │  └─ #changeNameBtn           48×48  @ (100, 0)      [Button, Image]
   ├─ #postIcon                   76×154 @ (-60, 70)     [Image, CSGUIImage]
   ├─ #discipleDescObj           248×45  @ (-245, 186)
   │  ├─ #discipleDescBg         248×45  @ (124, 22)     [Image, Button, VerticalLayoutGroup]
   │  └─ doFloatImage
   ├─ #orderBtn                   52×52  @ (-466, 200)   [Button, Image, CSGUIImage]
   ├─ #liandonBtn                 75×46  @ (-49, -55)    [Image, Button]
   ├─ #injuryObj                 215×36  @ (-245, -130)  [Image]
   │  ├─ clicker  ├─ #injuryIcon 32×32  ├─ #injuryTxt [Text 24 + Outline]  └─ #injuryBtn 198×36
   ├─ #ldLockObj                 336×28  @ (-250, -100)  [Image]
   │  ├─ clicker  ├─ ldIcon 32×27  └─ #ldTxt [Text 20 + Outline]
   ├─ #colorSign                  64×66  @ (-49, 200)    [Image, CSGUIImage]
   ├─ #colorSignbtn                75×75
   ├─ #showLihuiBtn               36×36  @ (-450, -85)
   ├─ buttonRoot                 470×145 @ (-252, -220)  [VerticalLayoutGroup]
   │  ├─ #fightroot              470×50
   │  ├─ #pdRoot                 330×55  @ (235, -77.5)  [Image]
   │  └─ #tmskillroot            470×86
   │     └─ #skillScrollView (ScrollRect) > Viewport > #skillContent
   │        ├─ #tmskillList      [CSGUICommonLayoutGroup] → tmskilltemp (1..5)
   │        └─ #dyskillList      [CSGUICommonLayoutGroup] → tmskilltemp (1..3)
   ├─ layout                      90×0   @ (-460, -203)  [VerticalLayoutGroup, ContentSizeFitter, LayoutElement]
   │  ├─ #switchBtn 91×82  ├─ #zhenfaBtn 91×82  └─ #signBtn 90×96 (+ #signBtnTx)
   ├─ #signEffect                100×100 @ (-374, 120)   [CSGUIEffect]
   └─ #guanlianBtn               104×84  @ (-45.8, -160)
      └─ #guanlianred             24×24
```

ข้อความ/ฟอนต์ (observed): 8 Text — `#discipleNameText` 24, `#discipleDesc` 22, `#injuryTxt` 24 + Outline, `#ldTxt` 20 + Outline, `#switchCdText` 18 + Outline, `#signBtnTx` 22, และ **`#discipleFightTxt` / `#pdVal` ใช้ bitmap font `font_fight_sprite2` (fontSize 0)** ไม่ใช่ Text ปกติ

---

## 4. Framework (observed จาก `Assembly-CSharp.dll` + Lua ที่แยกออกมา)

### 4.1 สายคลาส
`CSGUIWidgetBase` → `CSGUIWindowBase` → `CSGUIWindowLua` (`namespace Lua`) ; ฝั่ง Lua `UIWindowBase` (`gamecore.uibase.uiwindowbase`) เป็นตัวแทนหน้าต่างที่มี `winlua` ชี้กลับมาที่ `CSGUIWindowLua`

### 4.2 binding: เป็น index ไม่ใช่ชื่อ (สำคัญ)
- `CSGUIWidgetBase.ChildTransformArray` เป็น `public Transform[]` ที่ **serialize ไว้ใน prefab**; `GetChildTransform(int)` คืน `ChildTransformArray[index]`, index `-1` = ตัวเอง, เกินขอบ = log error + คืน `transform` (observed)
- `UIDiscipleRoleInfo2Win:bindComponents()` เรียก `UIButton.get(self,N) / UIText.get(self,N) / UIObject.get(self,N)` **เรียงตาม index 0..43** (observed ใน `gui.windows.disciple.uidiscipleroleinfo2win`)
- ตรวจ `ChildTransformArray` ของ prefab (44 ช่อง) เทียบกับชื่อใน Lua: **ตรงกันทั้ง 44 ตัว ไม่มี mismatch** และทั้ง 44 ตัวคือ child ที่ชื่อขึ้นต้นด้วย `#`
- สรุป: **`#` ไม่ใช่การ bind ด้วยชื่อ** — เป็นชื่อของ widget ที่ถูก export/ผูกเข้า Lua ตามลำดับใน `ChildTransformArray`; child ที่ไม่ขึ้นต้น `#` (เช่น `bg`, `lock`, `reddot`, `skillicon`, `mask`, `clicker`) เป็นของภายในที่หน้าต่างไม่เคยอ้างถึงตรง ๆ (observed: 44 hashed = 44 bound, 71 non-hashed ไม่ถูกอ้างในสคริปต์)

### 4.3 lifecycle
| ฝั่ง | สิ่งที่เกิดขึ้น | ที่มา |
|---|---|---|
| `UIWindowBase:show()` | ถ้า `isVisible` → `winlua:Show()`; ถ้าไม่ → `setVisible(true)` (= `winlua:SetVisible(true)`) | `gamecore.uibase.uiwindowbase` |
| `CSGUIWindowBase.Show()` | `SafeAreaApply()` → `DoShow()` → `SetActive(true)` + `DoShowAnimation()` (tween จาก `TweenUITransitionList`) | `CSGUI/CSGUIWindowBase.cs` |
| `UIWindowBase:close()` | `stopAllNotify()` → `winlua:Close()` → `closeAllWindow()` → `deleteSelf()` | `gamecore.uibase.uiwindowbase` |
| `CSGUIWindowBase.Close()` | `DoCloseAnimation()` ถ้า `WindowKillTime > 0` และยัง active; ไม่งั้น `CloseSelf()` (ทำลาย object หลัง `WindowKillTime` = 1.0 ของหน้าต่างนี้) | `CSGUIWindowBase.cs`, ค่าจาก prefab |
| `CSGUIWindowLua.SetVisible(bool)` | เปิด/ปิด `CanvasGroup.blocksRaycasts` และคืน/ซ่อนตาม `hideType` (`canvas` / `active` / `position` (±`OUT_OF_VIEW_3D`) / `scale`) | `Lua/CSGUIWindowLua.cs` |
| `CSGUIWindowLua.Awake/Start/OnDestroy` | `Awake` ลงทะเบียน `sLookup`, `Start` เรียก Lua `Start` ถ้า `_startEventFlag`, `OnDestroy` dispose lua table + `EVT_UI_WINDOW_DESTROY` | `Lua/CSGUIWindowLua.cs` |
| `UIDiscipleRoleInfo2Win:onLoaded` | `bindComponents()` + `addNotify(onDiscipleAttrChange/OrderChange/NameChange/StateChange/SignChange/SignTypeChange/InjuryChange)` | สคริปต์หน้าต่าง |
| `UIDiscipleRoleInfo2Win:__delete` | `doLocalMoveY(false)` ถ้า `isImageFloat`, `clearSwitchTimer()`, `unbindComponents()` | สคริปต์หน้าต่าง |

ค่าที่ serialize ใน prefab นี้: `hideType = 3`, `hideScale = (0,0,0)`, `WindowKillTime = 1.0`, `CanvasIndex = 3`, `windowPositionStyle = 5`, `LuaEventName = "OnEvent"`, `applySafeArea = 0`, `ExportSelfTransformToChild = 0`, `ChildTransformArray.Length = 44` (observed)

### 4.4 ตัวจัดการหน้าต่าง / เลเยอร์ / stack
- `UIManager:showWindowImp(name, argtable)` → ถ้า `active_win[name]` มีอยู่แล้ว: `win:show()` + `win:onShow(argtable,false)`; ถ้ายังไม่มี: `_LoadWindow(name, info.ab, -1, true)` (โหลด async, ใส่ `loading_win`, คืน `false` รอบแรก) (observed)
- `UIManager:closeWindowImp(name, forceClose)` → ถ้าแคชได้: `cache_window(name)` + `hideWindow(name)`; ไม่งั้น `_deleteWindow(win)`; `window_show_states[name]` ใช้ 0 = show, 1 = close, 2 = hide (observed)
- `UIManager:hideWindowImp(name)` → `onHide_before()` → `setVisible(false)` → `hideAllWindow()` → `onHide()` (observed)
- ผูกเลเยอร์: `get_window_config(name).canvas = 3` → `UIWindowBase:setParentCanvas(3)` → `UIManager:setCanvas(go,3)` → `CSGUIManager.AddUIToCanvas(ui, 3)` → `UICanvas[3]` = `GUI_ROOT/BottomUIContain/Canvas_BottomFirstLayer` (observed ครบทุกทอด)
- การปิด/stack: หน้าต่างแม่เก็บ `__childWindow` (show/close/hide ทีละชื่อ) ไม่มี stack กลางแบบ push/pop; ปุ่มย้อนกลับบน PC **ไม่พบ** handler กลางใน Lua/`Assembly-CSharp` (เจอแต่ `onBackPressed` ของ Android SDK) → ระบบ stack + Esc ใน my_clone เป็นของใหม่ (ข้อ 5)

### 4.5 ข้อมูลเข้า
- `UIDiscipleRoleInfo2Win:onShow(argtable)` อ่าน `argtable.guid` (และ `argtable.page`) แล้วเรียก `refreshBotton / refreshDiscipleInfo / refreshOrderBtn / refreshPos / refreshDiscipleSign` (observed)
- `onChangeDisciple(dis_guid)` = เรียก `onShow({guid=dis_guid})` ซ้ำ → เปลี่ยนลูกศิษย์โดยไม่เปิดหน้าต่างใหม่ (observed) — my_clone ทำแบบเดียวกันใน `SelectDisciple()`
- คนเปิด: `UIDiscipleMainWin` (หน้า Disciple หลัก) เปิดตาม page slot ด้วย `args={guid=self.disciple_guid, page=self.selectPage, subArgs=self.subArgs}`; ถ้าหน้าต่างเปิดอยู่แล้วเรียก `win:onShow(args)` ตรง ๆ (observed `gui.windows.disciple.uidisciplemainwin`)
- โมเดล: `comHelper.setChildInSideModel(self.discipleModelRoot, guid, 0.85, nil, 0,0,false,false,nil,{bgFisrt=true})` — สร้าง actor ใต้ `#discipleModelRoot` ด้วยสเกล 0.85 (observed)

---

## 5. สิ่งที่สร้างใหม่ใน my_clone

| ไฟล์ | หน้าที่ |
|---|---|
| `Assets/_Generated/Data/disciple_window.json` | ข้อมูล prefab ต่อ GameObject: ชื่อ, path, parentId, siblingIndex, active, RectTransform (anchorMin/Max, anchoredPosition, sizeDelta, pivot, localScale, localRotation), Image (sprite name + bundle, type, color, raycast, placeholderKind), Text (ข้อความ, ขนาด, สี, alignment, rich/bestFit, overflow, ฟอนต์), Button (transition, interactable, จำนวน persistent calls), CanvasGroup, Mask/RectMask2D, layout ทุกแบบ, LayoutElement, ContentSizeFitter, ScrollRect, Outline, UIModelShow + `skipped` (type ของเกมที่ข้าม) + `binding` (index → ชื่อ widget) |
| `Assets/Editor/UIWindowBuilder.cs` | อ่าน JSON → สร้าง prefab `Assets/_Generated/UI/DiscipleWindow.prefab` + scene `Assets/_Generated/UIDemo.unity`; มี 2 โหมด (placeholder / local sprites) + batch entry |
| `Assets/Scripts/UI/WindowBase.cs` | `Init/Open/Close` + `CanvasGroup` (alpha, interactable, blocksRaycasts) ตามพฤติกรรม `Show/Close/SetVisible` ที่ยืนยันแล้ว (ไม่ทำ tween/kill-time/lua) |
| `Assets/Scripts/UI/WindowManager.cs` | เลเยอร์ enum ตรงกับ `UICanvas` index (3..9), ย้าย parent ตามเลเยอร์, เก็บ stack, `Open/Close/CloseTop`, Esc = ปิดตัวบนสุด (**ของใหม่** ไม่ใช่พฤติกรรมเกม) |
| `Assets/Scripts/UI/DiscipleWindow.cs` | รับ mock list, เลือกลูกศิษย์ → อัปเดตชื่อ/สถานะ/กำลังรบ/แถวบาดเจ็บ/ปุ่มติดตาม/ไอคอนอาชีพ/จำนวนสกิล แล้วสลับ skin ของ SpineUiActor |
| `Assets/_Generated/UI/DiscipleWindow.prefab` | 115 GameObject ตาม dump + `SpineUiActor` ที่ `#discipleModelRoot` + `ModelPlaceholder` + `DiscipleWindow` (bind ครบ) |
| `Assets/_Generated/UIDemo.unity` | `UICamera` + `EventSystem` + `GUI_ROOT` (`WindowManager`, layers[3] = canvas) + `BottomUIContain/Canvas_BottomFirstLayer` + ตัวหน้าต่าง + `DemoBar` (overlay, sorting 5000) สำหรับสลับลูกศิษย์/ปิด/เปิด |

การแมป placeholder (โหมดเริ่มต้น): Image ใช้สีตาม `placeholderKind` (`panel` / `card` / `button` / `avatar` / `nameplate` / `invisible`) โดยไม่มี sprite ของเกม; `avatar` ที่ rect เกือบจัตุรัส (ratio 0.8–1.25) ได้สไปรต์วงกลมที่ **สร้างเองด้วยโค้ด** `Assets/_Generated/UI/Generated/round_placeholder.png` (128×128, ขอบ anti-alias) — 6 ตัว: `#discipleJobIcon`, `#discipleJobIcon2`, `#spBg`, `#injuryIcon`, `#colorSign`, `#guanlianred`; Text ใช้ Arial ของ Unity และ **สว่างขึ้นเฉพาะโหมด placeholder** เมื่อสีเดิมเข้มเกิน (โค้ด: `BuildText`, เงื่อนไข `Luminance < 0.45`)

สวิตช์เทียบภาพ: `Tools/UI/Build Disciple Window (Local Reference Sprites)` (เก็บสถานะใน `EditorPrefs` คีย์ `MyClone.UI.UseLocalReferenceSprites`) → ใช้ sprite ที่ดึงไว้ใต้ `Assets/_LocalReference/UI/Sprites` (24 ไฟล์, **gitignored**: `.gitignore` บรรทัด 175 `my_clone/Assets/_LocalReference/`; ตรวจด้วย `git check-ignore -v` แล้ว) ในโหมดนี้ Image 57/80 ได้ sprite จริง (`gray`/`lock`/`reddot`/`skillicon` บางตัวไม่มี sprite ใน bundle → ยังเป็นสี) ไฟล์ที่ได้จากเกมไม่เคยถูกคัดลอกเข้า prefab/scene

Component ของเกมที่ข้าม (อยู่ใน `skippedTypes` ของ build report): `CSGUIWindowLua`, `CSGUIImage`, `CSGUIBaseItem`, `CSGUICommonLayoutGroup`, `CSGUIEffect`, `UIModelShow` — `UIModelShow` ถูกแทนด้วย `SpineUiActor` ของเรา, `Outline` (Unity) ถูกสร้างจริงพร้อมค่า effectColor/effectDistance

Spine: `SpineUiActor` บน `#discipleModelRoot` ใช้ `Assets/_LocalReference/Portrait/2110101_SkeletonData.asset` (+ `2110101_Material`), skin เริ่มต้น `body_2110101_4_1`, animation `stand`;
**ข้อจำกัด**: asset ชุดนี้อยู่ใต้ `Assets/_LocalReference/` ซึ่ง gitignored — ในเครื่องที่ clone ใหม่ reference จะหาย และ `DiscipleWindow.ApplyModel` จะสลับไปใช้ `ModelPlaceholder` แทน (ไม่เรียก `Build` จึงไม่เกิด error log)
ตอนเลือกลูกศิษย์ หน้าต่างจะรวม skin ตามกลุ่ม (`body` / `face` / `face_acc` / `hair` / …) แล้วสลับ variant 0/1/2, ปรับสเกลให้สูง 98% ของ `#discipleModelRoot` (395 → ~386 px) และวางกึ่งกลาง

---

## 6. ตรวจผลจริง (Play mode, Unity 2021.3.45f2, Game view 1920×1080)

รันผ่าน MCP bridge ของ editor ที่เปิดอยู่ (ไม่มี exception ใน `Editor.log`):

| ตรวจ | ผล |
|---|---|
| เปิด scene แล้วเข้า Play | หน้าต่างเปิดเอง (`IsOpen=True`), parent = `Canvas_BottomFirstLayer`, rect 1334×750.4 (สเกลตาม width) |
| CanvasScaler | ที่หน้าจอ 1920×1080 ได้ canvas rect 1334×750.37 (สเกลตาม width ตาม `match=0`), shader channels 27 |
| ข้อมูลเริ่มต้น | name `Amy Robinson`, สถานะ `Secluded cultivation`, กำลังรบ `128,450`, `#injuryObj` ปิด, `fightroot` เปิด, `pdRoot` ปิด |
| Spine | `IsBuilt=True`, skin `clone_variant_0`, animation `stand`, สเกล 0.631 (พอดี `#discipleModelRoot`) |
| คลิกปุ่มใน DemoBar (ผ่าน `ExecuteEvents.pointerClick`) | เปลี่ยนเป็น `Li Qingyao`, `96,780`, แถวบาดเจ็บเปิด, skin `clone_variant_1`, สกิล 3/5 แถว |
| เลือกคนที่ 2 | `Mo Yan`, skin `clone_variant_2` |
| ปุ่ม Close / Reopen | ปิดจริง (`active=False`) และเปิดกลับได้ (stack = 1, คงตัวที่เลือกไว้) |
| Outline | 3 ตัวที่ `#injuryTxt`, `#ldTxt`, `#switchCdText` ค่า color/distance ตรง dump |
| Build ซ้ำ | `Tools/UI/Build...` รันได้ทั้ง 2 โหมด; โหมด local sprites แล้วกลับมา placeholder ได้ (final state = placeholder, sprite=0) |

### เทียบกับ screenshot
ในบริบทนี้ไม่มีไฟล์ภาพแนบมา จึง **ยังไม่ได้เทียบแบบตา/pixel** — ที่เทียบได้คือ "โครงสร้าง + พิกัด" กับ dump ซึ่งตรงทุก node (builder มี guard: จำนวน transform ที่สร้างต้องเท่ากับจำนวน node ใน dump ไม่งั้น throw)

สิ่งที่ตรงกับต้นฉบับเชิงโครงสร้าง: ลำดับ sibling ของ `#root` (Image 0, mask 1, Image#2 2, …), ชื่อ/ลำดับ child ทั้ง 115, ค่า RectTransform ทุกตัว, ตำแหน่ง/ขนาดของชื่อ-อาชีพ-ปุ่มชื่อ-มาสก์-โมเดล-การ์ดสถานะ-แถวบาดเจ็บ-ปุ่ม/เลย์เอาต์ด้านขวา, layout group/ContentSizeFitter ของ `layout` และ `buttonRoot`, anchor ของ canvas/scaler, เลเยอร์และลำดับการวาด

สิ่งที่ต่างจากต้นฉบับ (ต้องดูด้วยตาเมื่อเทียบภาพ):
1. ไม่มี art จริงในโหมดเริ่มต้น → สี/เงา/ขอบ/ไอคอนต่างจากภาพเกมทั้งหมด (โหมด local sprites ช่วยได้บางส่วน)
2. ฟอนต์: เกมใช้ฟอนต์ของเกม + bitmap font สำหรับเลขกำลังรบ (`#discipleFightTxt`, `#pdVal`) — my_clone ใช้ Arial ข้อความธรรมดา
3. สีตัวอักษรถูกสว่างขึ้นในโหมด placeholder (เพื่อให้อ่านได้บนพื้นสีเข้ม)
4. `UIModelShow` → `SpineUiActor`: สเกล/ตำแหน่งมาจากการ fit ของเรา (98% ของความสูง) ไม่ใช่ 0.85 ของ `setChildInSideModel`; ถ้าไม่มี SkeletonData จะเห็นกล่อง `MODEL (#discipleModelRoot)`
5. ไม่มี tween เปิด/ปิด (`WindowKillTime` 1.0 + `TweenUITransitionList`) และไม่มี `CSGUIEffect` ที่ `#signEffect`
6. สกิล: ต้นฉบับมี 5 ช่องตายตัว + สถานะล็อก/เทา/reddot ตามดาวและระดับ; my_clone แสดงจำนวนช่องตาม `skillCount` และ tint สี (ไม่มีข้อมูล config จริง)
7. `DemoBar` ด้านขวาเป็นของใหม่ (test harness) ไม่อยู่ในหน้าต่างจริง

---

## 7. ยืนยัน / อนุมาน / ไม่ทราบ

**ยืนยันแล้ว (observed)**
- prefab อยู่ใน bundle ข้างต้น, ชื่อราก `UIDiscipleRoleInfo2Win`, 115 GameObject, ค่า Canvas/Camera/Scaler ตามตารางข้อ 2
- binding ด้วย index เข้า `ChildTransformArray` (44 ตัว) และ `#`-prefix = widget ที่ถูก export (ตรงกัน 1:1 ทั้ง 44)
- เลเยอร์มาจาก `canvas=3` → `UICanvas[3]` = `Canvas_BottomFirstLayer`
- `onShow(argtable)` รับ `guid`/`page`; `onChangeDisciple(guid)` เรียก `onShow` ซ้ำ
- `onLoaded` = `bindComponents` + `addNotify` 7 ตัว; `__delete` = `clearSwitchTimer` + `unbindComponents`
- `CSGUIWindowBase.OUT_OF_VIEW_3D = (-10000,0,0)`; `hideType` ของหน้าต่างนี้ = 3; `WindowKillTime = 1.0`
- ตัวเปิดคือ `UIDiscipleMainWin` ตาม page slot (page 1 และ 3)

**อนุมาน (inferred)**
- `#`-prefix ถูกใส่โดยเครื่องมือ export ของทีมเกมเพื่อทำเครื่องหมาย "widget ที่ผูกกับ Lua" (ไม่มีโค้ดฝั่ง runtime อ่าน `#` เลย — `Assembly-CSharp` และ Lua ที่แยกได้ไม่มี `StartsWith("#")` หรือเทียบชื่อกับ `#`)
- `windowPositionStyle = 5` และ `hideType = 3` น่าจะเกี่ยวกับการจัดวาง/ซ่อนแบบ position+scale แต่ไม่ได้ตรวจเส้นทางโค้ดที่อ่านค่านี้
- `Canvas_BottomFirstLayer` ถูกใช้เป็นเลเยอร์หลักของหน้าต่างข้อมูล (bottom sheet) เพราะหน้าต่างกลุ่มเดียวกันใน config ส่วนใหญ่ใช้ `canvas=3` (ยกเว้น `UIDiscipleRoleInfoTwoWin` / `UIDiscipleRoleInfoChuiWeiWin` ที่ใช้ `canvas=4`)

**ไม่ทราบ (unknown)**
- ความหมาย/ที่มาของ `windowPositionStyle` ใน `CSGUIWindowLua` (ไม่ได้ตามโค้ดที่ใช้ค่านี้)
- พฤติกรรม `CSGUIEffect` ที่ `#signEffect` (พาร์ติเคิล/spine effect อะไร) และเงื่อนไขการเล่น
- ค่า config จริงของ `#switchBtn`/`#zhenfaBtn`/`#signBtn` (เงื่อนไขโชว์) — โค้ดอ่านจาก `UIDiscipleModel`/`UIDiscipleSign` ที่ต้องต่อเน็ต
- การ sort/stack ของหน้าต่างในเกมจริงในกรณีเปิดซ้อนหลายชั้น (มีแค่ `__childWindow` + `SetAsLastSibling` ที่ยืนยันได้)
- สาเหตุที่ `#signEffect` ไม่มี `CanvasGroup`/`RectMask2D` และผลต่อการ clip ในเครื่องจริง

---

## 8. ควรตรวจเพิ่มใน UnityExplorer (ในเกมจริง)

1. `Canvas_BottomFirstLayer` ขณะเกมรัน: `CanvasScaler.scaleFactor` จริงที่ความละเอียดต่าง ๆ (เช่น 16:10, ultrawide) — ยืนยันว่า match = width อย่างเดียว
2. `UIDiscipleRoleInfo2Win` (CSGUIWindowLua): ค่า runtime ของ `mIsVisible`, `mIsVisibleCull`, `WindowAnimation` และตอนกดปิดว่ามันย้ายไป −10000 หรือ `SetActive(false)`
3. `#discipleModelRoot`: ชื่อ child ที่ `UIModelShow` สร้าง, `localScale` จริงหลัง `setChildInSideModel(...,0.85,...)`, layer ของ actor
4. `#tmskillList` / `#dyskillList`: จำนวน child จริงตอนรัน (5 กับ 3?) และสถานะ `lock`/`reddot`/สีเทาในเคสมี/ไม่มีสกิล
5. `#discipleFightTxt` / `#pdVal`: ฟอนต์ bitmap (`font_fight_sprite2`) ใช้ atlas ไหน และสไปรต์เลขมาจาก bundle ใด
6. `#injuryObj`, `#ldLockObj`, `#signBtn`, `#switchBtn`, `#zhenfaBtn`, `#pdRoot`, `#fightroot`: `activeSelf` ตอนเปิดหน้าต่างของลูกศิษย์แต่ละประเภท (ปกติ/บาดเจ็บ/ชูเว่ย/ล็อก)
7. `#rolespbtn` และ `refreshDiscipleSign`: การเปลี่ยน `#colorSign` ตาม `getDiscipleColor` (ชื่อ sprite `image_pinjishibie_{n}`)
8. `UICamera` ในเกม: `clearFlags` (Depth) และ `cullingMask` ระหว่างเล่นจริง เพื่อยืนยันว่าไม่มีผลข้างเคียงกับ canvas อื่น

---

## 9. หน้าต่างที่ใช้โครงเดียวกัน (ชื่อเท่านั้น — ยังไม่ทำ)

ค่า `canvas` จาก `gui.uiwindowconfig_auto`:

| หน้าต่าง | bundle | canvas |
|---|---|---|
| `UIDiscipleRoleInfoWin` | `ui/windows/disciple/uidiscipleroleinfowin.ab` | 3 |
| `UIDiscipleRoleInfoTwoWin` | `ui/windows/disciple/uidiscipleroleinfotwowin.ab` | 4 |
| `UIDiscipleRoleInfoThreeWin` | `ui/windows/disciple/uidiscipleroleinfothreewin.ab` | 3 |
| `UIDiscipleRoleInfoChuiWeiWin` | `ui/windows/disciple/uidiscipleroleinfochuiweiwin.ab` | 4 |
| `UIDiscipleMainWin` (ตัวเปิด) | `ui/windows/disciple/uidisciplemainwin.ab` | 3 |
| `UIOtherDiscipleMainWin` | `ui/windows/disciple2/uiotherdisciplemainwin.ab` | 9 (DialogLayer) |
| `UIOtherDiscipleMainWin_SystemZongMen` | `ui/windows/systemzongmen/uiotherdisciplemainwin_systemzongmen.ab` | 9 |
| `UIFabaoDiscipleMainWin` | ไม่อยู่ใน `gui.uiwindowconfig_auto` (มีเฉพาะไฟล์ Lua `gui.windows.fabao.uifabaodisciplemainwin`) | ? |

(เป็นหน้าต่างที่ `UIDiscipleMainWin` ใช้ใน page slot เดียวกัน หรือหน้าต่าง main ของลูกศิษย์ตัวอื่น — ยังไม่ได้ตรวจ prefab ของแต่ละตัว)
