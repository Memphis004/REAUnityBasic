# ฉากสำนัก (Sect map) — สกัดจากเกมและสร้างใหม่ด้วย placeholder

> ขอบเขต: รอบนี้คือ "สกัดโครงสร้าง + สร้างใหม่ด้วย placeholder + บันทึกผล" เท่านั้น
> ไม่ได้เขียนระบบ build/วางตึก ไม่ได้ใช้ DLL, สคริปต์ หรือ art ของเกมในฉากที่สร้าง
> ทุกข้อแยกเป็น **observed** (อ่านได้ตรงจากไฟล์/ข้อมูล), **inferred** (อนุมานจากหลักฐาน), **unknown**

---

## 0. สรุปสั้น

| คำถาม | คำตอบ |
|---|---|
| สำนักเป็น scene หรือ prefab หรือสร้างจากข้อมูล? | **ทั้งคู่**: scene `scenemap/home/home.ab` มีแต่ Render/Lightmap/NavMesh settings ไม่มี GameObject เลย → แผนที่ถูก**ประกอบตอนรัน**จาก prefab `tilemap/map/hexgrid.ab` (asset `HexGrid`) + ตารางข้อมูลช่อง `mjmap.map.zongmen1` + ฉากหลัง 3D `tilemap/map/mountain1.ab` + เมฆ/พื้นหลัง `tilemap/mystery/*` |
| กล้อง | **orthographic** size 5 (Snap: ปรับได้ 4–6), near 0.3, far 6, depth 8, pos (0, 0, −4) + **กล้องหลัง perspective** fov 18°, depth 7, เอียง 14.5°, pos (−1000, −992, −27) |
| ขนาดแผนที่ | ตารางช่องครอบ **59.8 × 21.45 หน่วย** (23 × 13 ช่อง, ช่องละ 2.6 × 2.2) — เขตที่ลากกล้องได้ **60 × 44 หน่วย** (x −30…30, y −25.5…18.5) |
| ชั้นภาพ | 22 ชั้น `IT*` + Default/Entity/Effects… (40 ชั้นใน TagManager ของเกม) ลำดับตามข้อ 4 |
| parallax | **ไม่พบสคริปต์ parallax ตามกล้อง** — เมฆเป็น Animator + ตัวเกิดเมฆสุ่ม (`cloudsControl`) ใช้ `border` เดียวกับกล้อง (ข้อ 5) |

Scene ที่สร้าง: `Assets/_Generated/SectMapPlaceholder.unity` — 1,596 GameObject / 1,351 SpriteRenderer / 3.7 MB (ช่องแผนที่ 158 + place 1,143 + chunk mesh 12 ก้อนที่บรรจุช่องฉากหลัง 5,518 ช่อง), 43 ป้ายชื่อ (อีก 158 ป้ายของช่องถูกปิดไว้), 2 กล้อง

---

## 1. ตรวจว่าสำนักคืออะไร (observed)

**1.1 scene ของสำนัก** — `extracted/cfg_zongmen/sceneconfig.lua`

```
{ abname="scenemap/home/home.ab", assetname="home", id=2, resgroupid=2, scenecname="Sect" },
```

**1.2 ตัว scene ว่างจริง** — dump `scenemap/home/home.ab` ด้วย UnityPy ได้ object ทั้งหมด:

```
PreloadData, AssetBundle, RenderSettings pid=1, LightmapSettings pid=2, NavMeshSettings pid=3, LightingSettings pid=4
GameObjects = 0
```

**1.3 แผนที่ถูกโหลด/ประกอบตอนรัน** — `extracted/ab_text/binary/gamesys.lingmaizhenfengsystem.lingmaizhenfengcontroller_map.txt`

```
10: local HEX_AB="tilemap/map/hexgrid.ab"
11: local HEX_ASSET="HexGrid"
38: _HexMapManager.GetMainCameratransform().gameObject:SetActive(true)   -- เปิดกล้องแผนที่
39: _HexMapManager.GetBgCameratransform().gameObject:SetActive(true)
225: _HexMapManager.LoadMap(HEX_AB,HEX_ASSET,function() ... end)
240: _HexMapManager.InitMap()
243: _HexMapManager.SetCameraSize(view.cameraSize)
244: _HexMapManager.SetCameraBackgroudColor(view.backgroundColor)
245: _HexMapManager.SetGridSize(view.gridSize[1],view.gridSize[2],view.gridAngle)
249: _HexMapManager.LoadStage(stageName)
276: _HexMapManager.LoadTile(groupCfg.abname,groupCfg.resname,...)
299: _HexMapManager.Paint(Vector3(v[3][1],v[3][2],0),surfaceCfg.groupid,surface,HexMapLayer.Ground,v[4])
70–75: local worldPos=_HexMapManager.GetCellCenterWorld(Vector3(0,0,0),HexMapLayer.Ground)
        _HexMapManager.SetMapBorder(gridX*b[1], gridY*b[3], gridX*b[2], gridY*b[4], worldPos.x, worldPos.y, angle)
```

C# ที่รองรับ: `HexagonMapManagerInterface` / `MapManagerInterface` (`Assembly-CSharp.dll`, อ่านได้จาก ILSpy dump)

**1.4 ตารางช่อง/ตึก (สิ่งที่โจทย์ถามหา)** — `config.ab : mjmap.map.zongmen1` (JSON)

```
{"modID":"zongmen1","origin":{0,0,0},"sizeX":22,"sizeY":12,
 "cellInfo":[{"groupID":5,"cellID":101,"gridType":0,"pos":{"x":0,"y":0},
              "height":0.5,"roomX":-1,"roomY":-1,
              "entityInfo":[{"entityType":1,"extraGroupID":10000,"extraID":20002,"isHide":false}]}, ...]}
```

- 158 ช่องที่มีข้อมูล (จากกริด 23 × 13), `cellID` 101–110 (ชนิดพื้น), `height` ∈ {−0.5, 0, 0.5, 1.0}, `gridType` 0 = 157 ช่อง / 1 = 1 ช่อง, `roomX/roomY` = −1 ทั้งหมด
- 35 entity: `entityType` 1 ×25, 2 ×7, 3 ×1, 5 ×1, 6 ×1; `extraID` = 20001…20011 (อาคาร) และ 120001…120007, 31, 3
- ชื่อ/ขนาดอาคารจาก `rawdata.monijybuildconfig`: **ไม่พบ id 20001+ ในตารางนี้** (ตารางมี key 14…3012 และมีฟิลด์ `buid_size`, `height`, `build_sf`) → ขนาดอาคารแต่ละหลัง = **unknown**
- หลักฐานเชื่อมกับฉาก 3D: ชื่อ node ใน `mountain1.ab` คือ `20006_transmit`, `20006_MJ1/MJ2`, `2004_qiao`, `20004_qiao01..03` → ตรงกับ entity id 20004/20006

**1.5 ชั้นข้อมูลของแผนที่** — `extracted/cfg_zongmen/mnjyloadsceneconfig.lua` (22 ชั้น)

```
{ id=1, mlayer="Area1",  slayer="ITArea1",  mode="eChunk" }
{ id=3, mlayer="Grid",   slayer="ITGrid"   }
{ id=4, mlayer="Road1",  slayer="ITRoad1"  }
...
```

**1.6 prefab ที่ประกอบเป็นฉาก** (จาก `hexgrid.ab`, `mapcamera.ab`, `mountain1.ab`, `mystery/*`, `isogamesetting.ab`)

```
HexGrid                       [Transform, Grid, HexagonGameMap, DontDestroyOnLoad]
├── ground                    [Tilemap(0 tiles), TilemapRenderer]   sorting ITGround1
├── data                      [Tilemap(0), TilemapRenderer]         sorting ITBuilding
├── entitybirthpoint          [Tilemap(2×3), TilemapRenderer]       sorting ITBuilding
├── entitybirthpointTop       [Tilemap(0), TilemapRenderer]         sorting ITBuilding
├── entityRoot                [Transform]  (จุดเกิด entity)
├── roomground   (0,−100)     [Tilemap(6×5), TilemapRenderer]       sorting ITGround2
├── roomdata     (0,−100)     [Tilemap(0), TilemapRenderer]         sorting ITBuilding
├── roomground2  (0,−200)     [Tilemap(6×5), TilemapRenderer]       sorting ITBuilding
├── roomdata2    (0,−200)     [Tilemap(0), TilemapRenderer]         sorting ITBuilding
└── mapCamera (0,0,−4)  active=false  [Camera ortho 5, IsoCameraControl, CameraAdaptation, CameraScreenEffect, Wiggle]
    ├── bgCamera (−1000,−992,−27) rotX 14.5°  [Camera perspective]
    └── stageRoot (−1000,−1000,−4)            [HexagonMapStage]
```

- Tilemap ทั้งหมด **ว่างใน prefab** (0 tile) — เกมเพนต์ทีละช่องด้วย `LoadTile`/`Paint`
- กล้อง `mapCamera` ถูกปิดไว้ใน prefab และ Lua เปิดตอนเข้าแผนที่ (ข้อ 1.3 บรรทัด 38)

---

## 2. ค่าที่วัดได้ (observed, ตัวเลข)

### 2.1 กล้อง

| | MapCamera | BgCamera |
|---|---|---|
| projection | orthographic | perspective |
| size / fov | ortho size **5** (clamp 4…6 จาก `def_orthographic_size=[4,2,6]`) | **fov 18°** |
| clip | 0.3 – 6 | 0.3 – 180 |
| depth | 8 | 7 |
| clear | Depth (3) | Skybox (1) |
| position | (0, 0, −4) | (−1000, −992, −27) → world z −31 |
| rotation | (0,0,0) | (**14.5**, 0, 0) |
| viewport / culling | 1×1, ทุก layer (0xFFFFFFFF) | 1×1, ทุก layer |
| background color | #314D79 (0.192, 0.302, 0.475) | — |
| script | `IsoCameraControl` useEase=1 ease=6 (OutQuart) easeTime=0.15, `CameraAdaptation`, `CameraScreenEffect` | `CameraAdaptation` |

`mapcamera.ab` เป็น prefab กล้องชุดเดียวกันแบบ standalone (ortho 5, far 1000, depth −1, clear Color #000000)

### 2.2 ตาราง/ขนาดแผนที่

| | ค่า |
|---|---|
| Grid cell size | **2.6 × 2.2**, `cellLayout = 1 (Hexagon)`, `cellSwizzle = XYZ` |
| ตารางช่อง | 23 × 13 ช่อง (x 0…22, y 0…12) = 158 ช่องที่มี tile |
| ขนาดรวม (คำนวณ) | **59.8 × 21.45 หน่วย** (23×2.6 กว้าง, 13×2.2×0.75 สูง) |
| เขตที่กล้องลากได้ | **60 × 44 หน่วย** — x −30…30, y −25.5…18.5 (`monijysfconfig` id=1 "Main Peak") |
| ตำแหน่งกล้องเริ่มต้น | `def_camera_pos = [−6, −39, 0]` (ค่านี้อยู่นอก border — ดูข้อ 7 unknown) |
| ขนาดตึก | **unknown** (id 20001+ ไม่อยู่ใน `monijybuildconfig`) |
| IsoGameSetting (grid ผิว/3D) | `cellSize = (1.0, 0.5)`, `cellLayout = 3`, `cellPosOffset = (0, −0.25, 0)`, `cullingDisatance = 30` |

### 2.3 ฉากหลัง 3D (`mountain1.ab`)

- 2,449 GameObject / **2,339 MeshRenderer** / 125 SortingGroup / 7 Animator / 8 MonoBehaviour
- กลุ่มหลัก: `ThisMapOnly_01` (1,625 objs), `ThisMapOnly_02` (34), `ThisMapOnly_03` (751), `510463` (14), `510474` (24)
- ขอบเขตจากตำแหน่ง node: x −103.46…64.82, y −43.28…26.11 → **168.3 × 69.4 หน่วย**
- ฝัง TextAsset `map_data_1` (172 KB) = ตำแหน่ง tile ผิวของฉากนี้ + `SurfaceManager` อ่านมัน (ถอดได้แล้ว — ข้อ 2.5)

### 2.5 ตาราง tile ของฉากหลัง (`mountain1.ab : TextAsset map_data_1`) — observed

```json
{ "positionList":[{"x":2,"y":-21,"z":0}, …5518], "idList":[3, …5518],
  "placePositionList":[{"x":-9,"y":-34,"z":0}, …1143], "placeIdList":[230, …1143] }
```

- เป็น **array คู่ขนาน 2 ชุด**: `positionList` ↔ `idList` (ช่องผิว) และ `placePositionList` ↔ `placeIdList` (วัตถุที่วางบนผิว)
- 5518 ช่อง (ไม่ซ้ำพิกัดเลย), 1143 place
- ช่องครอบ cell x −92…59 (148 ค่า), y −97…43 (110 ค่า) — พิกัดเป็น **หน่วยช่อง** ไม่ใช่หน่วย world
- `idList` 34 ค่า: 1–24 (ผิวปกติ), 101/102/104/105, และ 121/122/141/142/192/210 (ค่าละ 1 ช่อง)
- `placeIdList` 185 ค่า (0…3004); พบบ่อย: 234 ×130, 232 ×63, 235 ×48, 233 ×38, 242 ×31, 407 ×25
- **การแปลงเป็น world (inferred, มีหลักฐาน)**: `world = (x · 1.0, y · 0.5 − 0.25)` ตาม `IsoGameSetting.cellSize = (1.0, 0.5)` + `cellPosOffset.y = −0.25`
  → ช่องครอบ **148 × 55 หน่วย** และ **100% ของช่องอยู่ในกรอบ mesh ของ mountain1** (ถ้าใช้ 1:1 จะได้แค่ 81%) ; y span 55 หน่วยใกล้เคียง mesh 69.4
  → มี 2 ช่องที่ยังหลุดกรอบ (y = −97 → world −48.75 ต่ำกว่า mesh −43.28): น่าจะเป็นช่องของส่วนที่ถูกตัด/ซ่อน
- **ความหมายของ id** — ตามรอยได้ในข้อ 2.6

### 2.6 tile id 1–24 / 101–105 และ place id 0–3004 คืออะไร

**กลไก (observed จาก C#/ILSpy)**
- `SurfaceManager` (component บน `mountain1`) เก็บ `mapDataJson` (= TextAsset `map_data_1`), `dataTile` (asset `MapDataTileBase`), `partList`
- `GameMap.AddData(GameMapData)`: `positionList[i] → idList[i]` เก็บเป็น `m_CellDataIdDictionary[pos] = id`; `placePositionList[j] → placeIdList[j]` เก็บเป็น `m_PlaceDataIdDictionary[pos] = id`
- ตอนอัปเดต tile: `HexagonGameMap.GetCellAttr(pos, layer)` → `HexagonalMapDataTile.GetCellConfigByID(id)` → `HexCellAttr`
  ```csharp
  public class HexagonalMapDataTile : TileBase {          // UnityEngine.HexagonalMapDataTile
      public int m_GroupID; public Sprite m_Sprite;
      public List<HexCellAttr> cellConfigList;
  }
  public class HexCellAttr {                              // IsoTilemapGame
      int ID; Color color; OutputSprite m_Output; Sprite sprite; Sprite[] m_Sprites;
      float m_Speed, m_StartTime; int m_EntityType;
  }
  ```
  → **tile id = `HexCellAttr.ID` ของ tile-set ที่ใช้ในฉากนี้** (ไม่ใช่ enum กลาง) ; `m_Output` = None/Single/Animation/Random และ `m_Speed/m_StartTime` ใช้กับน้ำ/ไฟที่เล่นแอนิเมชัน ; `m_EntityType` ใช้ผูก entity
- ตอนวางของ: `GameMap.GetPrePlaceData(id)` → `((MapDataTile)dataTile).GetPreConfigByID(id)` → `PrePlaceData{ ID, configID, flip, color }`
  แล้ว `LoadSundriseByArea` เรียก `onSundriseCreate(mapId, areaId, prePlaceData.configID, prePlaceData.flip, pos)`
  → **place id = `PrePlaceData.ID` ของ `preConfigList` ใน dataTile เดียวกัน** ; ของจริงคือ `configID`
- รันไทม์ของจริงมาจากเซิร์ฟเวอร์: `sundriseCreateControl:findDataAndCreate` → `zongmenModel:getSundriseDataByPos(mapId,x,y,id)` และ `data.buildid` → `cfg_monijybuildconfig_get` (`extracted/ab_text/binary/gamesys.home.sundrisecreatecontrol.txt:470`)

**หลักฐานว่า id เป็น "per-asset" ไม่ใช่ค่ากลาง** — ถอด `HexagonalMapDataTile` จาก bundle ที่มีในเครื่อง (`tilemap/tileassets/data/`):

| asset | `m_GroupID` | `cellConfigList` IDs |
|---|---|---|
| `HexagonalMapSurfaceTile2` | 2 | 41–49 (9) |
| `HexagonalMapSurfaceTile3` | 3 | 1001 (1) |
| `HexagonalMapPublicTile` | 0 | 0, −1, −2, −3 |

=> เลขเดียวกัน (เช่น 3) คนละ asset หมายถึงคนละผิว ; `m_GroupID` ตรงกับชื่อใน `secretscentsurfacegroupconfig` (`HexagonalMapSurfaceTile2..13`, `HexagonalMapPublicTile`) ที่ถอดได้จาก binary

**ทำไมยังบอกชื่อ id 1–24 ไม่ได้**: `mountain1` อ้าง `dataTile` เป็น external (`m_FileID: 6`) ซึ่งชี้ `CAB-fa2e5d332ad2a53a34140ea547883480` — สแกน `.ab` ทั้ง 613 ไฟล์ในเครื่องนี้แล้ว **ไม่พบ** (เจอแค่ `CAB-01cc42…` = `shader.ab`); 5 ใน 6 external ของ mountain1 หายไป → ตาราง `cellConfigList`/`preConfigList` ตัวจริงไม่อยู่ในเครื่อง

**สิ่งที่บอกได้จากข้อมูลเอง (observed, จากรูปทรงการกระจาย)**

| id | จำนวน | bbox (ช่อง) | fill | ลักษณะ |
|---|---|---|---|---|
| 1–24 | 102–408 | 9–29 × 9–27 | 0.62–1.00 | **ผืนพื้นที่** (terrain patch) — id 21 เต็ม 0.99, id 19 เต็ม 1.00 |
| 101 | 127 | 136 × 131 | 0.01 | **เส้นทาง/แนวเส้น** พาดทั้งแมพ (ไม่ใช่ผืน) |
| 105 | 104 | 30 × 19 | 0.18 | แนวโปร่งขนาดกลาง (รั้ว/คูน้ำ?) |
| 104 / 102 | 8 / 2 | 30×36 / 37×56 | 0.01 / 0 | เส้นบางยาว |
| 121,122,141,142,192,210 | 1 each | 1×1 | — | จุดเดี่ยว (marker) |

**place id**: 0–3004, 185 ค่า, 1,143 จุด — ตรวจแล้ว **ไม่ใช่** id ใน `monijybuildconfig` (ทับกันแค่ 4/185 ค่า ≈ 2%) จึงยืนยันได้แค่ว่าเป็น `PrePlaceData.ID` ของ dataTile ที่หายไป ; `configID` จริงจะไปโผล่เป็น `buildid` ของ sundry จากเซิร์ฟเวอร์

**inferred (มีหลักฐานสนับสนุน)**: `entityType` ใน `mjmap.map.zongmen1` (ค่าที่พบ 1,2,3,5,6) ตรงกับ enum `sundriseType` ใน `isometricmapsystem.lua:157` — 1=eStillSundrise (ของนิ่ง), 2=eMovementSundrise (ของเคลื่อนที่), 3=eStillEnemy, 5=eScenery, 6=eRewardBox ; ตัวเลข 25/7/1/1/1 ตัวก็เข้าท่า (ของนิ่งเยอะสุด) แต่ยังไม่มีโค้ดยืนยันการแมปตรง ๆ → **inferred**

### 2.4 sprite ของฉาก (เมฆ/พื้นหลัง/ผิว)

| object | sprite | px / PPU | scale | sorting | order |
|---|---|---|---|---|---|
| `MapCloud1/SpriteObject` | 580206 | 300×144 / 50 | 1.00 | ITSky1 | 1 |
| `MapCloud1/SpriteObject1` | 580210 | 350×172 / 50 | 0.715 | ITSky1 | 1 |
| `MapCloud1/SpriteObject2` | 580210 | 350×172 / 50 | 0.892 | ITSky1 | 1 |
| `MapBackGround/SpriteObject` | (sprite อยู่นอก bundle) | m_Size 300 × 46.17 | 1.00 | ITGround1 | 1 |
| `Surface1/SpriteObject` | (sprite อยู่นอก bundle) | m_Size 2.56 × 7.2 | 1.00 | ITGround1 | 5 |

ไม่มี Canvas/UI ใน prefab ชุดนี้ (UI อยู่ใน bundle `ui/*`)

---

## 3. ชั้นภาพ (sorting layers) — observed

จาก `globalgamemanagers : TagManager.m_SortingLayers` (40 ชั้น) เรียงล่าง→บน; ลำดับที่แผนที่ใช้:

```
ITWater1 → ITWater2 → ITGround1 → ITGround2 → ITDecoration1..4 → ITArea1 → ITArea2
→ ITGrid → ITRoad1 → ITRoad2 → ITDrawCollect → ITDrawRoad → ITBuilding → ITGrid2
→ ITSkyBD → ITDrag → ITSky1 → ITUI
```

ก่อนหน้า ITWater1 มีชั้นอื่นของเกม (Background, Terrain_(−2..2), FakeShadow, Default) และหลัง ITUI มี Entity, Effects, MeshSkyBox, CanvasBottom, UIBottom, UIWindow, UITop, UITopModel…

ใน prefab จริงใช้แค่ 3 ชั้น: `ITGround1` (ground, roomground, mapbackground, surface), `ITGround2` (roomground2), `ITBuilding` (data/entity/roomdata), `ITSky1` (เมฆ)

**ในโปรเจกต์เรา**: builder เพิ่ม 21 ชื่อนี้ลง `ProjectSettings/TagManager.asset` ตามลำดับของเกม (ชื่อตรง, `uniqueID` เป็นค่าที่เราคำนวณเอง — ดูข้อ 7)

---

## 4. parallax / เมฆ — observed + inferred

- **ไม่พบสคริปต์ parallax ที่ผูกกับกล้อง** ใน prefab: `HexTilemapObject` มีแค่ field `m_Animator`; เมฆเคลื่อนด้วย **AnimatorController `MapCloud1`** (clips: `MapCloud1`, `MapCloud1_1`, `MapCloud1_2`)
- ตัวเกิดเมฆตอนรัน: `extracted/lua_zongmen/cloudscontrol.lua` — ใช้ `cfg_cloudconfig` (minCreateTime/maxCreateTime/initial/fadeTime/minFlyTime/maxFlyTime/minAlpha/maxAlpha) และสุ่มเกิดใน `border` ของ `monijysfconfig` ±10 หน่วย:

```
local border=cfg.border
self.cloudBorder={left=border[1]-10, top=border[2], right=border[3]+10, bottom=border[4]}
```

- **inferred**: เมฆไม่ใช่ parallax ตามระยะกล้อง แต่เป็น sprite เคลื่อนที่ (Animator/tween) ในระนาบเดียวกับแผนที่ → ฉาก placeholder จึงวางเมฆเป็น sprite นิ่งตามตำแหน่ง/สเกลจริง **ไม่ใส่ parallax**
- ค่าความเร็ว/สัดส่วน parallax: **unknown / ไม่มีในเกม**

---

## 5. สิ่งที่สร้างใหม่ (`Assets/_Generated/SectMapPlaceholder.unity`)

Pipeline (รันซ้ำได้ทั้งหมด):

```
1) python extracted/dump_sect_scene.py   -> Assets/_Generated/Data/sect_scene_dump.json   (หลักฐานดิบต่อ GameObject)
2) python extracted/build_sect_layout.py -> Assets/_Generated/Data/sect_layout.json       (JSON แบนสำหรับ JsonUtility)
3) Unity -executeMethod MyClone.EditorTools.SectSceneBuilder.BuildBatch                  -> สร้างฉาก
   Unity -executeMethod MyClone.EditorTools.SectSceneBuilder.VerifyBatch                 -> ตรวจฉาก (exit code ≠ 0 ถ้าผิด)
```

หรือใน Editor: menu **Tools/Sect/Build Map Placeholder Scene**

โครงที่สร้าง (ตัวเลข = ค่าจริงจากข้อ 2):

```
SectMap
├── Cameras
│   ├── MapCamera      Camera ortho 5 / near .3 / far 6 / depth 8 / clear Depth / #314D79 / tag MainCamera
│   │                  + SectCameraPanZoom (ลาก/ซูม, clamp border −30…30 × −25.5…18.5, zoom 4…6)
│   └── BgCamera       Camera perspective fov 18 / far 180 / depth 7 / clear Skybox / (−1000,−992,−27) rotX 14.5°
├── HexGrid            Grid cellSize 2.6×2.2, layout Hexagon
│   ├── Layers         10 โหนดตามชื่อเดิม (ground, data, entitybirthpoint, …, roomdata2) + แผ่นโปร่งแสงบอกขอบเขต tilemap
│   ├── Cells          158 สี่เหลี่ยมสีตาม cellID (ป้ายพิกัดถูกปิดไว้ เปิดได้ใน Hierarchy)
│   └── Entities       35 สี่เหลี่ยมสีตาม entityType + ป้าย "<id>" (และ hint เช่น 20006_transmit)
├── Background         ที่ (−1000,−1000,−4)
│   ├── Tiles          5,518 ช่องจาก map_data_1 รวมเป็น **12 chunk mesh** (quad + vertex color, 1.0 × 0.5 หน่วย/ช่อง)
│   ├── Places         1,143 วัตถุจาก placePositionList (0.8 × 0.8 หน่วย, สีตาม place id)
│   └── GroupMarkers   ป้ายชื่อ 5 กลุ่ม mountain1 + 20006_transmit (ไม่มีกล่องแล้ว)
└── Parallax
    ├── MapBackGround  300 × 46.17
    ├── MapCloud1      3 เมฆ (ขนาด px/50×scale)
    └── Surface1       2.56 × 7.2
```

ขนาด/สี: สี่เหลี่ยม `White.png` (8×8 ขาว, PPU 100) ย้อมสี — ไม่มี asset ของเกมแม้แต่ไฟล์เดียวในฉาก
ป้ายชื่อ: TextMesh (ฟอนต์ builtin ของ Unity) — 43 ป้ายที่มองเห็น (158 ป้ายของช่องปิดไว้กันทับกัน)

ผลตรวจ (`sect_scene_verify_report.json`): `errors = 0`, กล้อง 2, ortho 5, ช่อง 158/158, entity 35, layer root 10, stage tiles 5518/5518, places 1143/1143, stage (−1000,−1000,−4)

ขนาดฉาก: **1,596 GameObject / 1,351 SpriteRenderer / 3.7 MB** — ช่องฉากหลัง 5,518 ช่องถูกรวมเป็น **12 chunk mesh** (`Assets/_Generated/SectMap/Chunks/StageChunk_x_y.asset`, รวม 1.8 MB) ใช้ material เดียว `SectVertexColor.mat` (shader `Sprites/Default` + สีจาก vertex) ; ก่อนรวมเป็น 7,102 object / 16.5 MB

---

## 6. เทียบกับต้นฉบับ (screenshot ภาพที่ 1)

**ยังเทียบไม่ได้ — ยังไม่ได้รับภาพ** (ในโจทย์เขียนว่า "ผมจะส่งให้")

สิ่งที่เทียบได้แล้วตอนนี้คือ "โครงสร้างกับตัวเลข" ไม่ใช่ภาพ:

| หัวข้อ | ต้นฉบับ | ของเรา | สถานะ |
|---|---|---|---|
| ชนิดกล้อง/ขนาด | ortho 5, near .3 far 6, z −4 | เท่ากัน | ตรง |
| กล้องหลัง | perspective 18°, 14.5°, (−1000,−992,−31) | เท่ากัน | ตรง |
| กริด | 2.6×2.2 hex, 23×13 | เท่ากัน | ตรง |
| จำนวนช่อง/ตึก | 158 / 35 | เท่ากัน | ตรง |
| ช่องผิวฉากหลัง | 5,518 (+1,143 place) | เท่ากันทีละช่อง (รวมเป็น 12 mesh) | ตรง (ตำแหน่ง/จำนวน), สี/id เป็นการเดา |
| ชั้นภาพ | ITGround1/2, ITBuilding, ITSky1 | เท่ากัน (ชื่อ+ลำดับ) | ตรง (ID ต่าง — ข้อ 7) |
| สี/พื้นผิว/รูปทรง | 3D mesh 2,339 ชิ้น | ช่องสี 5,518 + place 1,143 | **ต่างโดยเจตนา** (placeholder, แต่ตำแหน่ง/จำนวนตรง) |
| เมฆ | Animator + เกิดใหม่สุ่ม | sprite นิ่ง 3 ก้อน | ต่างโดยเจตนา |

เมื่อได้ภาพจริง ให้เทียบ 4 อย่างนี้ก่อน: (1) สัดส่วนพื้นที่แผนที่ต่อจอ, (2) ระดับการเอียง/ความสูงของภูเขา, (3) ความหนา/สีของเมฆ, (4) UI ทับ (ยังไม่ทำ)

---

## 7. inferred / unknown

### inferred
- ลำดับการประกอบฉาก: `LoadMap(hexgrid)` → `InitMap` → `SetGridSize/SetCameraSize` → `LoadStage(mountain1)` → `LoadTile` + `Paint` ต่อช่อง — อ่านจากลำดับการเรียกใน Lua (ข้อ 1.3)
- ระยะช่องใช้สูตร hex pointy-top: `x=(cx+0.5·(cy&1))·2.6`, `y=cy·2.2·0.75` → กว้าง 59.8 ใกล้ border 60 (Unity `Grid` ใช้ 3/4 offset สำหรับ hex) — **สมมติฐานนี้ใช้ใน builder**
- `mountain1` คือ stage ที่วางใต้ `stageRoot` (−1000,−1000,−4) แล้วถูกมองด้วย BgCamera (perspective) ขณะที่ HexGrid อยู่ที่ origin และถูกมองด้วย MapCamera (ortho)
- entity id 20001–20011 = อาคารในสำนัก (บางตัวแมปกับ node ใน `mountain1` ได้: 20004 = สะพาน, 20006 = จุดวาร์ป)

### unknown (ต้องตรวจเพิ่มใน UnityExplorer)
1. **ขนาดตึกแต่ละหลัง** — ไม่มี 20001+ ใน `monijybuildconfig`; ต้องดู runtime `MapManager` entity list หรือ `mjmap` entity config ตัวจริง
2. **`def_camera_pos = [−6, −39, 0]`** อยู่นอก border (−25.5…18.5) — น่าจะเป็น offset เฉย ๆ หรือเป็นพิกัดของ "จุดสร้างตึก" ไม่ใช่กล้อง; ต้องดู `cameraMoveController` ตอนรัน
3. **การวาง hex จริง** — ยืนยันจาก `_HexMapManager.GetCellCenterWorld` ตอนรัน (คอลัมน์/แถว offset, `cellPosOffset.y = −0.25` ของ `IsoGameSetting`)
4. **`sky_layout_area` = x −85…66, y −80…66 (151×146)** ไม่ตรงกับขอบ mesh ของ `mountain1` (168×69) — ต้องดูว่ามี stage อื่น (`mountain1_wx`) หรือสเกลตอนโหลด
5. **`stageName` ของ Main Peak** — `lingmaizhengfengmapconfig` (มี `cameraSize`, `backgroundColor`, `border`, `gridSize`) ไม่ได้ถูกสกัดออกมา; ค่าที่ใช้ใน builder มาจาก `monijysfconfig` แทน
6. **สีพื้นหลังจริง** ของ MapCamera ตอนเล่น (ค่าใน prefab #314D79 แต่ clear = Depth)
7. **`uniqueID` ของ sorting layer**: ของเกม เช่น `ITBuilding = 1142199635`; ของเราใช้ FNV-1a ของชื่อ (ค่าต่างกัน) — ชื่อ+ลำดับตรง แต่ ID ไม่ตรง (ไม่กระทบการเรียงในโปรเจกต์เรา)
8. **Canvas/UI ของหน้าสำนัก** — อยู่นอกขอบเขต prefab นี้
9. **ชื่อ/สไปรต์ของ tile id 1–24, 101–105 และ place id 0–3004** — ต้องได้ asset `dataTile` ของ mountain1 (CAB-fa2e5d… ที่ไม่มีในเครื่อง) จึงจะอ่าน `cellConfigList`/`preConfigList` ได้; ตอนนี้รู้แค่กลไกและ "รูปทรง" ของแต่ละ id (ข้อ 2.6)
10. **`positionList` ที่ y = −97 (2 ช่อง)** อยู่นอกกรอบ mesh — ต้องดูตอนรันว่าเป็นช่องที่ซ่อนหรือเป็น data ค้าง

---

## 8. ไฟล์ที่แตะในรอบนี้

**โปรเจกต์ (my_clone)**
- `Assets/Editor/SectSceneBuilder.cs` — อ่าน `sect_layout.json` → สร้างฉาก + เพิ่ม sorting layer + `VerifyBatch`
- `Assets/Scripts/Sect/SectCameraPanZoom.cs` — ลาก/ซูมขั้นต่ำ (เขียนใหม่, clamp ตาม border/zoom ของเกม)
- `Assets/_Generated/SectMapPlaceholder.unity` — ฉาก placeholder (สร้างจาก builder)
- `Assets/_Generated/SectMap/White.png` — สี่เหลี่ยมขาว 8×8 ที่ builder สร้างเอง
- `Assets/_Generated/SectMap/Chunks/StageChunk_*.asset` (12) — mesh ของช่องฉากหลัง (quad + vertex color)
- `Assets/_Generated/SectMap/SectVertexColor.mat` — material เดียวที่ทุก chunk ใช้
- `Assets/_Generated/Data/sect_scene_dump.json` — โครงสร้างดิบต่อ GameObject (หลักฐาน)
- `Assets/_Generated/Data/sect_layout.json` — ข้อมูลที่ builder ใช้
- `Assets/_Generated/Data/sect_scene_build_report.json`, `sect_scene_verify_report.json`
- `ProjectSettings/TagManager.asset` — เพิ่ม 21 sorting layer (ชื่อ/ลำดับตามเกม)

**เครื่องมือสกัด (นอกโปรเจกต์ Unity)**
- `extracted/dump_sect_scene.py` — UnityPy dump ต่อ GameObject (Camera/SpriteRenderer/Tilemap/Grid/Canvas/MonoBehaviour typetree) + ถอด TextAsset `map_data_1` เป็น `stageMap`
- `extracted/build_sect_layout.py` — แบนข้อมูลเป็น JSON สำหรับ JsonUtility
- `extracted/lua_tables.py` — parser ตาราง Lua (resolve `__rs`/`__r_N`/`__rs[i]`)

**ไม่ได้ทำ (ตามขอบเขตที่กำหนด)**: ระบบ build/วางตึก, ระบบ entity, การโหลด tile จริง, UI, parallax, และการเทียบภาพกับ screenshot

---

## 9. หมายเหตุการรัน

- ตอนรัน batch มี Unity Editor เปิดโปรเจกต์นี้อยู่ (`Temp/UnityLockfile`) จึงรัน build/verify ในสำเนาที่ `%LOCALAPPDATA%/Temp/sectbuild_project` แล้วคัดลอกเฉพาะ artifact (ฉาก, png, meta, TagManager, report) กลับมา — GUID ของ `SectCameraPanZoom` และ `White.png` ในฉากตรงกับ `.meta` ที่คัดลอกมาด้วย (ตรวจแล้ว)
- ถ้าจะรันซ้ำ: ปิด Editor หรือใช้ menu `Tools/Sect/Build Map Placeholder Scene`
