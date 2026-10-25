def write_index(doc):
    rows = []
    for s in doc["skeletons"]:
        rows.append({
            "id": s["id"],
            "originalName": s["originalName"],
            "thumb": s["thumb"] or "",
            "category": s["category"],
            "categoryGuessed": s["categoryGuessed"],
            "categoryBasis": s["categoryBasis"],
            "group": s["group"],
            "family": s["family"],
            "groupMeaning": s["groupMeaning"],
            "groupConfidence": s["groupConfidence"],
            "bones": s["bones"],
            "slots": s["slots"],
            "skins": s["skins"],
            "animations": s["animations"],
            "skinNames": s["skinNames"],
            "animationNames": s["animationNames"],
            "pages": s["atlasPages"],
            "pageList": [{"page": p["page"], "w": p["width"], "h": p["height"],
                          "bytes": p["pngBytes"]} for p in s["pages"]],
            "textureSize": s["textureSize"],
            "version": s["spineVersion"] or "",
            "versionOk": s["versionOk"],
            "bytes": s["fileBytesWithTextures"],
            "status": s["status"],
            "error": s["error"],
            "bundle": s["bundle"],
            "configEvidence": s["configEvidence"],
            "alternates": s["alternates"],
        })

    payload = json.dumps({"skeletons": rows, "count": doc["count"]},
                         ensure_ascii=False)
    group_options = "".join(
        '<option value="%s">%s &mdash; %s (%d)</option>'
        % (g["group"], g["group"], g["meaning"], g["skeletons"])
        for g in doc["prefixGroups"])

    html = """<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Spine library - __COUNT__ skeletons</title>
<style>
  :root { color-scheme: dark; }
  * { box-sizing: border-box; }
  body { margin: 0; background: #14161c; color: #e6e9f2;
         font: 14px/1.45 system-ui, "Segoe UI", Tahoma, sans-serif; }
  header { padding: 18px 22px 10px; border-bottom: 1px solid #2a2f3c; position: sticky; top: 0;
           background: #14161c; z-index: 3; }
  h1 { margin: 0 0 6px; font-size: 20px; font-weight: 600; }
  .sub { color: #97a0b5; font-size: 12px; }
  .controls { display: flex; gap: 10px; flex-wrap: wrap; margin-top: 12px; }
  input, select { background: #1e2230; color: #e6e9f2; border: 1px solid #333a4b;
                  border-radius: 6px; padding: 7px 10px; font: inherit; }
  input[type=search] { min-width: 220px; }
  main { padding: 14px 22px 40px; }
  table { border-collapse: collapse; width: 100%; }
  th, td { text-align: left; padding: 8px 10px; border-bottom: 1px solid #242936;
           vertical-align: middle; }
  th { color: #aab3c7; font-weight: 600; font-size: 12px; text-transform: uppercase;
       letter-spacing: .04em; position: sticky; top: 132px; background: #14161c; z-index: 1; }
  tbody tr { cursor: pointer; }
  tbody tr:hover td { background: #1a1e28; }
  img.thumb { width: 64px; height: 64px; object-fit: contain; background: #0d0f14;
              border: 1px solid #262c3a; border-radius: 6px; }
  .id { font-family: Consolas, "Courier New", monospace; font-size: 13px; }
  .bad { color: #ff8a8a; }
  .ok { color: #7fd6a4; }
  .dim { color: #8c95a9; }
  .pill { display: inline-block; padding: 1px 7px; border-radius: 99px; background: #232937;
          font-size: 11px; color: #b9c3d8; }
  #count { color: #b9c3d8; margin-left: 6px; font-weight: 400; font-size: 13px; }
  td.anims { max-width: 320px; color: #8c95a9; font-size: 12px; }
  #detail { position: fixed; top: 0; right: 0; width: min(520px, 46vw); height: 100%;
            background: #191c25; border-left: 1px solid #2f3646; padding: 18px 20px 40px;
            overflow-y: auto; transform: translateX(105%); transition: transform .18s ease;
            z-index: 5; box-shadow: -18px 0 40px rgba(0,0,0,.35); }
  #detail.open { transform: translateX(0); }
  #detail h2 { margin: 0 0 4px; font-size: 17px; font-family: Consolas, monospace; }
  #detail h3 { margin: 18px 0 6px; font-size: 12px; text-transform: uppercase;
               letter-spacing: .05em; color: #aab3c7; }
  #detail .row { color: #9aa4b9; font-size: 12px; margin: 2px 0; }
  #detail .chips { display: flex; flex-wrap: wrap; gap: 5px; }
  #detail .chip { background: #232937; border: 1px solid #333a4b; border-radius: 5px;
                  padding: 2px 7px; font-size: 12px; font-family: Consolas, monospace; }
  #detail .close { position: absolute; top: 10px; right: 12px; background: #262c3a;
                   border: 0; color: #dfe5f2; border-radius: 5px; padding: 3px 9px;
                   cursor: pointer; font: inherit; }
  #detail .warn { color: #ffd479; }
  #backdrop { position: fixed; inset: 0; background: rgba(0,0,0,.35); opacity: 0;
              pointer-events: none; transition: opacity .18s; z-index: 4; }
  #backdrop.open { opacity: 1; pointer-events: auto; }
</style>
</head>
<body>
<header>
  <h1>Spine library <span id="count"></span></h1>
  <div class="sub">Read-only extraction of the game's Spine skeletons. Thumbnail = first atlas page.
    Prefix meanings are checked against dbbodyconfig/dbcomponentcfg where possible; rows say when
    the category is still a guess. Versions other than 3.8.x are flagged.</div>
  <div class="controls">
    <input id="q" type="search" placeholder="Search id / bundle / category">
    <input id="nameq" type="search" placeholder="Filter by skin or animation name">
    <select id="group"><option value="">All prefixes</option>__GROUPS__</select>
    <select id="category"><option value="">All categories</option></select>
    <select id="version"><option value="">All Spine versions</option>
      <option value="ok">3.8.x only</option><option value="bad">non-3.8.x only</option></select>
    <select id="evidence"><option value="">All category sources</option>
      <option value="config">confirmed by config</option>
      <option value="guess">bundle-only guess</option></select>
  </div>
</header>
<main>
  <table>
    <thead><tr>
      <th></th><th>Id</th><th>Category</th><th>Bones / slots</th><th>Skins / anims</th>
      <th>Atlas</th><th>Spine</th><th>Size</th><th>Examples</th>
    </tr></thead>
    <tbody id="tbody"></tbody>
  </table>
</main>
<div id="backdrop"></div>
<aside id="detail"><button class="close" id="closeDetail">close</button><div id="detailBody"></div></aside>
<script>
const DATA = __DATA__;
const tbody = document.getElementById('tbody');
const q = document.getElementById('q');
const nameq = document.getElementById('nameq');
const groupSel = document.getElementById('group');
const catSel = document.getElementById('category');
const verSel = document.getElementById('version');
const evSel = document.getElementById('evidence');
const detail = document.getElementById('detail');
const backdrop = document.getElementById('backdrop');
const detailBody = document.getElementById('detailBody');
const countLabel = document.getElementById('count');
countLabel.textContent = DATA.count + ' skeletons';

const cats = [...new Set(DATA.skeletons.map(s => s.category))].sort();
for (const c of cats) {
  const o = document.createElement('option');
  o.value = c; o.textContent = c;
  catSel.appendChild(o);
}

function kb(n) { return n >= 1048576 ? (n/1048576).toFixed(1)+' MB' : Math.round(n/1024)+' KB'; }
function esc(s) { return String(s).replace(/[&<>"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c])); }
function chips(list) {
  if (!list || !list.length) return '<div class="row">(none)</div>';
  return '<div class="chips">' + list.map(n => '<span class="chip">' + esc(n) + '</span>').join('') + '</div>';
}
function namesText(s) { return (s.skinNames || []).concat(s.animationNames || []); }

function matchesName(s, needle) {
  if (!needle) return true;
  return namesText(s).some(n => n.toLowerCase().includes(needle));
}

function render() {
  const text = q.value.trim().toLowerCase();
  const needle = nameq.value.trim().toLowerCase();
  const g = groupSel.value, c = catSel.value, v = verSel.value, ev = evSel.value;
  const frag = document.createDocumentFragment();
  let shown = 0, total = 0;
  for (const s of DATA.skeletons) {
    if (g && s.group !== g) continue;
    if (c && s.category !== c) continue;
    if (v === 'ok' && !s.versionOk) continue;
    if (v === 'bad' && s.versionOk) continue;
    if (ev === 'config' && s.categoryGuessed) continue;
    if (ev === 'guess' && !s.categoryGuessed) continue;
    if (text && !(s.id + ' ' + s.bundle + ' ' + s.category + ' ' + s.groupMeaning).toLowerCase().includes(text)) continue;
    if (!matchesName(s, needle)) continue;
    total++;
    if (shown >= 2500) continue;
    shown++;
    const tr = document.createElement('tr');
    tr.innerHTML =
      '<td>' + (s.thumb ? '<img class="thumb" loading="lazy" src="' + esc(s.thumb) + '" alt="">' : '') + '</td>' +
      '<td class="id">' + esc(s.id) + '<div class="dim">' + esc(s.bundle) + '</div></td>' +
      '<td>' + esc(s.category) + (s.categoryGuessed ? ' <span class="pill" title="bundle-only guess">guess</span>' : '') +
        '<div class="dim">' + esc(s.groupMeaning) + '</div></td>' +
      '<td>' + (s.bones == null ? '<span class="dim">?</span>' : s.bones + ' / ' + s.slots) + '</td>' +
      '<td>' + (s.skins == null ? '<span class="dim">?</span>' : s.skins + ' / ' + s.animations) + '</td>' +
      '<td>' + s.pages + '</td>' +
      '<td class="' + (s.versionOk ? 'ok' : 'bad') + '">' + esc(s.version || '?') + '</td>' +
      '<td>' + kb(s.bytes) + '</td>' +
      '<td class="anims">' + esc((s.animationNames || []).slice(0, 6).join(', ')) +
        ((s.animationNames || []).length > 6 ? ' …' : '') + '</td>';
    tr.addEventListener('click', () => showDetail(s));
    frag.appendChild(tr);
  }
  tbody.innerHTML = '';
  tbody.appendChild(frag);
  countLabel.textContent = total + ' of ' + DATA.count + ' skeletons' +
    (total > shown ? ' (showing first ' + shown + ')' : '');
}

function showDetail(s) {
  const ev = s.configEvidence || {};
  let html = '<h2>' + esc(s.id) + '</h2>' +
    '<div class="row">' + esc(s.originalName || '') + '</div>' +
    '<div class="row">bundle: ' + esc(s.bundle) + '</div>' +
    '<div class="row">version: ' + esc(s.version || '?') +
      (s.versionOk ? ' <span class="ok">(3.8.x)</span>' : ' <span class="bad">(not 3.8.x)</span>') + '</div>' +
    '<div class="row">bones ' + s.bones + ' · slots ' + s.slots + ' · skins ' + s.skins +
      ' · animations ' + s.animations + '</div>' +
    '<div class="row">atlas ' + s.pages + ' page(s), ' + esc(s.textureSize || '-') + '</div>' +
    '<div class="row">files ' + kb(s.bytes) + '</div>' +
    '<h3>Category</h3>' +
    '<div class="row">' + esc(s.category) + (s.categoryGuessed ? ' <span class="warn">(guess)</span>' : ' <span class="ok">(config-confirmed)</span>') + '</div>' +
    '<div class="row">' + esc(s.categoryBasis || '') + '</div>' +
    '<div class="row">prefix ' + esc(s.group) + ' / family ' + esc(s.family) + ': ' + esc(s.groupMeaning) +
      ' <span class="dim">(' + esc(s.groupConfidence) + ')</span></div>' +
    '<h3>Config evidence</h3>' +
    '<div class="row">dbbodyconfig rows: ' + (ev.dbbodyconfigRows || 0) +
      ' · dbcomponentcfg rows: ' + (ev.dbcomponentcfgRows || 0) + '</div>' +
    (ev.followSlots && ev.followSlots.length ? '<div class="row">followSlot: ' + esc(ev.followSlots.join(', ')) + '</div>' : '') +
    '<h3>Skins (' + (s.skinNames || []).length + ')</h3>' + chips(s.skinNames) +
    '<h3>Animations (' + (s.animationNames || []).length + ')</h3>' + chips(s.animationNames) +
    '<h3>Atlas pages</h3>' +
    (s.pageList && s.pageList.length
      ? '<div class="row">' + s.pageList.map(p => esc(p.page) + ' (' + p.w + '×' + p.h + ', ' + kb(p.bytes) + ')').join('<br>') + '</div>'
      : '<div class="row">(none)</div>') +
    (s.error ? '<h3>Error</h3><div class="row warn">' + esc(s.error) + '</div>' : '') +
    (s.alternates && s.alternates.length
      ? '<h3>Alternates</h3><div class="row">' + s.alternates.map(a => esc(a.bundle)).join('<br>') + '</div>'
      : '');
  detailBody.innerHTML = html;
  detail.classList.add('open');
  backdrop.classList.add('open');
}
function closeDetail() {
  detail.classList.remove('open');
  backdrop.classList.remove('open');
}
document.getElementById('closeDetail').addEventListener('click', closeDetail);
backdrop.addEventListener('click', closeDetail);
document.addEventListener('keydown', e => { if (e.key === 'Escape') closeDetail(); });

for (const el of [q, nameq, groupSel, catSel, verSel, evSel]) el.addEventListener('input', render);
render();
</script>
</body>
</html>
"""
    html = (html.replace("__COUNT__", str(doc["count"]))
                .replace("__GROUPS__", group_options)
                .replace("__DATA__", payload))

    with open(os.path.join(OUT, "index.html"), "w", encoding="utf-8") as f:
        f.write(html)


if __name__ == "__main__":
    sys.exit(main())
