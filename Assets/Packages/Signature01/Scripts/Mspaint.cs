using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Mspaint : MonoBehaviour 
{

    private Color paintColor = Color.red;
	private float paintSize = 0.3F;
	private LineRenderer currentLine;	
	private List<Vector3> positions=new List<Vector3>();
    private List<GameObject> lines = new List<GameObject>();
	private bool isMouseDown = false;
	private Vector3 lastMousePostion = Vector3.zero;
	private float lineDistance = 0.1F;


    public Material m_LineMaterial;
    public Text m_SizeValue;

	void Update()
    {
		if (Input.GetMouseButtonDown (0))
        {
			GameObject go = new GameObject ();
           
			go.transform.SetParent (this.transform);
			currentLine = go.AddComponent<LineRenderer> ();
            currentLine.material = m_LineMaterial;
			currentLine.startWidth = paintSize;
			currentLine.endWidth = paintSize;
			currentLine.startColor = paintColor;
			currentLine.endColor = paintColor;
			currentLine.numCornerVertices = 10;
			currentLine.numCapVertices = 10;
			Vector3 position = GetMousePoint ();
			AddPosition (position);
			isMouseDown = true;
			lineDistance += 0.1F;
            lines.Add(go);
		}
		if (isMouseDown) 
        {
			Vector3 position = GetMousePoint ();
            if (Vector3.Distance(position, lastMousePostion) > 0.05F)
            {
                AddPosition(position);
            }				
		}

		if (Input.GetMouseButtonUp (0)) 
        {
			currentLine = null;
			positions.Clear ();
			isMouseDown = false;
		}
	}

    /// <summary>
    /// 添加要的画线包含的点
    /// </summary>
    /// <param name="position"></param>
	void AddPosition(Vector3 position)
    {
		position.z -= lineDistance;
		positions.Add (position); 
		currentLine.positionCount = positions.Count;
		currentLine.SetPositions (positions.ToArray ());
		lastMousePostion = position;
	}

    /// <summary>
    /// 将鼠标的屏幕坐标投影到世界空间中
    /// </summary>
    /// <returns></returns>
	Vector3 GetMousePoint()
    {
		Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		RaycastHit hit;
		bool isCollider = Physics.Raycast (ray, out hit);
		if (isCollider) {
			return hit.point;
		}
		return Vector3.zero;
	}

	#region 设置线条的颜色及粗细
	public void OnRedColorChanged(bool isOn )
    {
		if (isOn)
        {
			paintColor = Color.red;
		}
	}
	public void OnGreenColorChanged(bool isOn)
    {
		if (isOn) 
        {
			paintColor = Color.green;
		}
	}
	public void OnBlueColorChanged(bool isOn)
    {
		if (isOn)
        {
			paintColor = Color.blue;
		}
	}

    public void OnSizeChanged(float value)
    {
        paintSize = value;
        m_SizeValue.text = value.ToString();
    }

    public void OnClearBtnClick()
    {
        for (int i = 0; i < lines.Count; i++)
        {
            Destroy(lines[i]);
        }
        lines.Clear();
        lineDistance = 0.1F;
    }

	#endregion
}
