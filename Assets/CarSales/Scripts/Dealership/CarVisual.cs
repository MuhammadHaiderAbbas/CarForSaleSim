using UnityEngine;

namespace CarSales
{
    /// <summary>Put on the root of every car MODEL prefab. Applies paint colour and dirt.</summary>
    public class CarVisual : MonoBehaviour
    {
        [SerializeField] Renderer[] paintRenderers;            // body panels
        [SerializeField] int paintMaterialIndex = 0;
        [SerializeField] string colorProperty = "_BaseColor";  // URP Lit. Built-in Standard = "_Color"
        [SerializeField] string dirtProperty = "_Dirt";        // optional custom Shader Graph property
        static readonly Color DirtColor = new Color(0.35f, 0.3f, 0.25f);
        MaterialPropertyBlock mpb;

        public void Apply(Color paint, float dirt01)
        {
            if (mpb == null) mpb = new MaterialPropertyBlock();
            Color shown = Color.Lerp(paint, DirtColor, dirt01 * 0.5f); // simple "dirty" look
            foreach (var r in paintRenderers)
            {
                if (!r) continue;
                r.GetPropertyBlock(mpb, paintMaterialIndex);
                mpb.SetColor(colorProperty, shown);
                mpb.SetFloat(dirtProperty, dirt01);
                r.SetPropertyBlock(mpb, paintMaterialIndex);
            }
        }
    }
}
