using System;
using UnityEngine;
using UnityEditor;

namespace Igor
{
    public class VisualScriptingDrawing
    {
        public static Texture2D aaLineTex = null;
        public static Texture2D lineTex = null;
    	
    	public static Texture2D GenerateTexture2DWithColor(Color InColor)
    	{
    		Texture2D NewTexture = new Texture2D(1, 1, TextureFormat.ARGB32, true);
    		NewTexture.SetPixel(0, 1, Color.Lerp(InColor, Color.white, 0.0f));
    		NewTexture.Apply();
    		
    		return NewTexture;
    	}
    	
    	public static Texture2D TintTextureWithColor(Texture2D InTexture, Color TintColor, float TintAmount)
    	{
    		Texture2D TintedTexture = (Texture2D)Texture2D.Instantiate(InTexture);
    		
    		Color[] PixelData = TintedTexture.GetPixels();
    		
    		for(int CurrentPixel = 0; CurrentPixel < PixelData.Length; ++CurrentPixel)
    		{
    			PixelData[CurrentPixel] = Color.Lerp(PixelData[CurrentPixel], TintColor, TintAmount);
    		}
    		
    		TintedTexture.SetPixels(PixelData);
    		
    		TintedTexture.Apply();
    		
    		return TintedTexture;
    	}
    	
        public static void curveFromTo(Rect inwr, Rect inwr2, Color color, Color shadow, Vector2 Offset)
    	{
    		Rect wr = new Rect(inwr.x-Offset.x, inwr.y-Offset.y, inwr.width, inwr.height);
    		Rect wr2 = new Rect(inwr2.x-Offset.x, inwr2.y-Offset.y, inwr2.width, inwr2.height);
    		
            Vector3 startPos = new Vector3(wr.x + wr.width, wr.y + 3 + wr.height / 3, 0);
            Vector3 endPos = new Vector3(wr2.x, wr2.y + wr2.height / 2, 0);
    		
            float mnog = Vector3.Distance(startPos,endPos);

            Vector3 startTangent = startPos + Vector3.right * (mnog / 3f) ;
            Vector3 endTangent = endPos + Vector3.left * (mnog / 3f);

            Handles.BeginGUI();
            Handles.DrawBezier(startPos, endPos, startTangent, endTangent,color, null, 3f);
            Handles.EndGUI();
    	}
    		
    	public static void Unused(Rect inwr, Rect inwr2, Color color, Color shadow, Vector2 Offset)
        {
    		Rect wr = new Rect(inwr.x-Offset.x, inwr.y-Offset.y, inwr.width, inwr.height);
    		Rect wr2 = new Rect(inwr2.x-Offset.x, inwr2.y-Offset.y, inwr2.width, inwr2.height);
    		VisualScriptingDrawing.bezierLine(
                new Vector2(wr.x + wr.width, wr.y + 3 + wr.height / 2),
                new Vector2(wr.x + wr.width + Mathf.Abs(wr2.x - (wr.x + wr.width)) / 2, wr.y + 3 + wr.height / 2),
                new Vector2(wr2.x, wr2.y + 3 + wr2.height / 2),
                new Vector2(wr2.x - Mathf.Abs(wr2.x - (wr.x + wr.width)) / 2, wr2.y + 3 + wr2.height / 2), shadow, 5, true,20);
    		VisualScriptingDrawing.bezierLine(
                new Vector2(wr.x + wr.width, wr.y + wr.height / 2),
                new Vector2(wr.x + wr.width + Mathf.Abs(wr2.x - (wr.x + wr.width)) / 2, wr.y + wr.height / 2),
                new Vector2(wr2.x, wr2.y + wr2.height / 2),
                new Vector2(wr2.x - Mathf.Abs(wr2.x - (wr.x + wr.width)) / 2, wr2.y + wr2.height / 2), color, 2, true,20);
        }
    	
        public static void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width, bool antiAlias)
        {
            Color savedColor = GUI.color;
            Matrix4x4 savedMatrix = GUI.matrix;
            
            if (!lineTex)
            {
                lineTex = new Texture2D(1, 1, TextureFormat.ARGB32, true);
                lineTex.SetPixel(0, 1, Color.white);
                lineTex.Apply();
            }
            if (!aaLineTex)
            {
                aaLineTex = new Texture2D(1, 3, TextureFormat.ARGB32, true);
                aaLineTex.SetPixel(0, 0, new Color(1, 1, 1, 0));
                aaLineTex.SetPixel(0, 1, Color.white);
                aaLineTex.SetPixel(0, 2, new Color(1, 1, 1, 0));
                aaLineTex.Apply();
            }
            if (antiAlias) width *= 3;
            float angle = Vector3.Angle(pointB - pointA, Vector2.right) * (pointA.y <= pointB.y?1:-1);
            float m = (pointB - pointA).magnitude;
            if (m > 0.01f)
            {
                Vector3 dz = new Vector3(pointA.x, pointA.y, 0);

                GUI.color = color;
                GUI.matrix = translationMatrix(dz) * GUI.matrix;
                GUIUtility.ScaleAroundPivot(new Vector2(m, width), new Vector3(-0.5f, 0, 0));
                GUI.matrix = translationMatrix(-dz) * GUI.matrix;
                GUIUtility.RotateAroundPivot(angle, Vector2.zero);
                GUI.matrix = translationMatrix(dz + new Vector3(width / 2, -m / 2) * Mathf.Sin(angle * Mathf.Deg2Rad)) * GUI.matrix;

                if (!antiAlias)
                    GUI.DrawTexture(new Rect(0, 0, 1, 1), lineTex);
                else
                    GUI.DrawTexture(new Rect(0, 0, 1, 1), aaLineTex);
            }
            GUI.matrix = savedMatrix;
            GUI.color = savedColor;
        }

        public static void bezierLine(Vector2 start, Vector2 startTangent, Vector2 end, Vector2 endTangent, Color color, float width, bool antiAlias, int segments)
        {
            Vector2 lastV = cubeBezier(start, startTangent, end, endTangent, 0);
            for (int i = 1; i <= segments; ++i)
            {
                Vector2 v = cubeBezier(start, startTangent, end, endTangent, i/(float)segments);

                VisualScriptingDrawing.DrawLine(
                    lastV,
                    v,
                    color, width, antiAlias);
                lastV = v;
            }
        }

        private static Vector2 cubeBezier(Vector2 s, Vector2 st, Vector2 e, Vector2 et, float t){
            float rt = 1-t;
            float rtt = rt * t;
            return rt*rt*rt * s + 3 * rt * rtt * st + 3 * rtt * t * et + t*t*t* e;
        }

        private static Matrix4x4 translationMatrix(Vector3 v)
        {
            return Matrix4x4.TRS(v,Quaternion.identity,Vector3.one);
        }
    	
    	public static bool RectOverlapsRect(Rect RectA, Rect RectB)
    	{
    		return RectAOVerlapsRectB(RectA, RectB) || RectAOVerlapsRectB(RectB, RectA);
    	}
    	
    	public static bool RectAOVerlapsRectB(Rect RectA, Rect RectB)
    	{
    		if(RectA.x >= RectB.x && RectA.x <= (RectB.x + RectB.width))
    		{// RectA's left side is inside RectB horizontal range
    			if(RectA.y >= RectB.y && RectA.y <= (RectB.y + RectB.height))
    			{// RectA's top is inside RectB vertical range
    				return true;
    			}
    			else if(RectA.y+RectA.height >= RectB.y && RectA.y+RectA.height <= (RectB.y + RectB.height))
    			{// RectA's bottom is inside RectB vertical range
    				return true;
    			}
    		}
    		if(RectA.x+RectA.width >= RectB.x && RectA.x+RectA.width <= (RectB.x + RectB.width))
    		{// RectA's right side is inside RectB horizontal range
    			if(RectA.y >= RectB.y && RectA.y <= (RectB.y + RectB.height))
    			{// RectA's top is inside RectB vertical range
    				return true;
    			}
    			else if(RectA.y+RectA.height >= RectB.y && RectA.y+RectA.height <= (RectB.y + RectB.height))
    			{// RectA's bottom is inside RectB vertical range
    				return true;
    			}
    		}
    		
    		return false;
    	}
    }
}