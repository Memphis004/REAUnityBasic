var ctrl = UnityEngine.Object.FindObjectOfType<MyClone.LocalReference.SpineDemoController>();
var p = ctrl.portrait;
var sb = new System.Text.StringBuilder();
string S() {
    var g = p.Graphic;
    var rt = (UnityEngine.RectTransform)g.transform;
    return "flipX=" + p.FlipX + " graphicScale=" + rt.localScale.x + " anchored=" + rt.anchoredPosition + " size=" + rt.sizeDelta + " actorScale=" + p.transform.localScale.x;
}
sb.Append("before: ").Append(S());
ctrl.ToggleFlip();
sb.Append("\nafter : ").Append(S());
ctrl.ToggleFlip();
sb.Append("\nback  : ").Append(S());
return sb.ToString();
