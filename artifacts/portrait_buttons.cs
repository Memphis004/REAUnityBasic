var sb = new System.Text.StringBuilder();
var ctrl = UnityEngine.Object.FindObjectOfType<MyClone.LocalReference.SpineDemoController>();
if (ctrl == null || ctrl.portrait == null) return "no controller/portrait";
var p = ctrl.portrait;
UnityEngine.Application.logMessageReceived += (m, s, t) => { };
int Drawn() { int n=0; for(int i=0;i<p.Skeleton.Slots.Count;i++) if (p.Skeleton.Slots.Items[i].Attachment!=null) n++; return n; }
string State() {
    var cur = p.AnimationState.GetCurrent(0);
    return "scaleX=" + p.Skeleton.ScaleX + " gray=" + p.IsGray + " mat=" + (p.Graphic.material==null?"null":p.Graphic.material.shader.name) +
        " skin=" + (p.Skeleton.Skin==null?"default":p.Skeleton.Skin.Name) + " anim=" + (cur==null||cur.Animation==null?"-":cur.Animation.Name) +
        " verts=" + p.Graphic.MeshGenerator.Buffers.vertexCount + " drawn=" + Drawn();
}
sb.Append("before: ").Append(State());
ctrl.ToggleFlip();
sb.Append("\nflip  : ").Append(State());
ctrl.ToggleGray();
sb.Append("\ngray  : ").Append(State());
ctrl.NextSkin();
sb.Append("\nskin+1: ").Append(State());
ctrl.NextAnimation();
sb.Append("\nanim+1: ").Append(State());
ctrl.ToggleGray();
ctrl.ToggleFlip();
sb.Append("\nrestore: ").Append(State());
return sb.ToString();
