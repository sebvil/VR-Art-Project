/// <summary>
/// CodeArtist.mx 2015
/// This is the main class of the project, its in charge of raycasting to a model and place brush prefabs infront of the canvas camera.
/// If you are interested in saving the painted texture you can use the method at the end and should save it to a file.
/// </summary>


using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum MyPainter_BrushMode{PAINT,DECAL};
public class MyTexturePainter : MonoBehaviour {
	public GameObject brushCursor,brushContainer; //The cursor that overlaps the model and our container for the brushes painted
	public Camera sceneCamera,canvasCam;  //The camera that looks at the model, and the camera that looks at the canvas.
	public Sprite cursorPaint,cursorDecal; // Cursor for the differen functions 
	public Material baseMaterial; // The material of our base texture (Were we will save the painted texture)
    public GameObject paper;
    static int Period { get; set; } = 300;

    MyPainter_BrushMode mode; //Our painter mode (Paint brushes or decals)
	float brushSize=0.2f; //The size of our brush
	Color brushColor; //The selected color
	int brushCounter=0,MAX_BRUSH_COUNT=1000; //To avoid having millions of brushes
	bool saving=false; //Flag to check if we are saving the texture
    int time = 0;
    int dt = 1;
    Color[] colors = { Color.yellow, Color.blue, Color.red };
    Vector3[] painted_locations = new Vector3[Period];
    int writeIndex = 0;
    int readIndex = 0;
    float erase_loc = 0;
    bool erase = false;

    RenderTexture canvasTexture; // Render Texture that looks at our Base Texture and the painted brushes


    void Start()
    {
        Renderer rend = paper.GetComponent<Renderer>();
        RenderTexture tex = new RenderTexture(canvasCam.targetTexture);
        Material m = new Material(rend.material)
        {
            mainTexture = tex
        };
        rend.material = m;
        canvasCam.targetTexture = tex;
        canvasTexture = tex;
    }
    void Update () {
		brushColor = colors[(time / (Period / 6)) % 3];	//Updates our painted color with the selected color
        time += dt;
       
       
        if (time % Period == 0)
        {
            erase = true; 
        }
        if (time % Period == 6)
        {
            erase = false;
            erase_loc = 0;
        }
    /*    if (erase)
        {
            brushColor = Color.white;
            EraseAction();
            erase_loc += 0.1f;
        }*/
        if (Input.GetMouseButton(0))
        {
            DoAction();
        }
        writeIndex++;
        writeIndex = writeIndex % Period;
        UpdateBrushCursor ();
	}

	//The main action, instantiates a brush or decal entity at the clicked position on the UV map
	void DoAction(){	
		if (saving)
			return;
		Vector3 uvWorldPosition=Vector3.zero;		
		if(HitTestUVPosition(ref uvWorldPosition)){
			GameObject brushObj;
			if(mode==MyPainter_BrushMode.PAINT){

				brushObj=(GameObject)Instantiate(Resources.Load("TexturePainter-Instances/BrushEntity")); //Paint a brush
				brushObj.GetComponent<SpriteRenderer>().color=brushColor; //Set the brush color
			}
			else{
				brushObj=(GameObject)Instantiate(Resources.Load("TexturePainter-Instances/DecalEntity")); //Paint a decal
			}
			brushColor.a= 1; // Brushes have alpha to have a merging effect when painted over.
			brushObj.transform.parent=brushContainer.transform; //Add the brush to our container to be wiped later
			brushObj.transform.localPosition=uvWorldPosition; //The position of the brush (in the UVMap)
			brushObj.transform.localScale=Vector3.one*brushSize;//The size of the brush
		}

	}

    void EraseAction()
    {
        Debug.Log("erasing");
        if (saving)
            return;

                Vector3 uvWorldPosition = new Vector3(erase_loc, erase_loc, 0);
           
            if (uvWorldPosition != null)
            {
                GameObject brushObj;
                if (mode == MyPainter_BrushMode.PAINT)
                {

                    brushObj = (GameObject)Instantiate(Resources.Load("TexturePainter-Instances/EraseEntity")); //Paint a brush
                    brushObj.GetComponent<SpriteRenderer>().color =brushColor; //Set the brush color
                }
                else
                {
                    brushObj = (GameObject)Instantiate(Resources.Load("TexturePainter-Instances/DecalEntity")); //Paint a decal
                }
                brushColor.a = 1; // Brushes have alpha to have a merging effect when painted over.
                brushObj.transform.parent = brushContainer.transform; //Add the brush to our container to be wiped later
                brushObj.transform.localPosition = uvWorldPosition; //The position of the brush (in the UVMap)
                brushObj.transform.localScale = Vector3.one * brushSize;//The size of the brush
            }
        

    }
    //To update at realtime the painting cursor on the mesh
    void UpdateBrushCursor(){
		Vector3 uvWorldPosition=Vector3.zero;
		if (HitTestUVPosition (ref uvWorldPosition) && !saving) {
			brushCursor.SetActive(true);
			brushCursor.transform.position =uvWorldPosition+brushContainer.transform.position;									
		} else {
			brushCursor.SetActive(false);
		}		
	}
	//Returns the position on the texuremap according to a hit in the mesh collider
	bool HitTestUVPosition(ref Vector3 uvWorldPosition){
		RaycastHit hit;
		Vector3 cursorPos = new Vector3 (Screen.width / 4, Screen.height / 2, 0.0f);
        Debug.Log(cursorPos);
		Ray cursorRay=sceneCamera.ScreenPointToRay (cursorPos);
		if (Physics.Raycast(cursorRay,out hit,200)){
			MeshCollider meshCollider = hit.collider as MeshCollider;
			if (meshCollider == null || meshCollider.sharedMesh == null)
				return false;			
            if (!GameObject.ReferenceEquals(meshCollider.gameObject, paper))
            {
                return false;
            }
			Vector2 pixelUV  = new Vector2(hit.textureCoord.x,hit.textureCoord.y);
			uvWorldPosition.x=pixelUV.x-canvasCam.orthographicSize;//To center the UV on X
			uvWorldPosition.y=pixelUV.y-canvasCam.orthographicSize;//To center the UV on Y
			uvWorldPosition.z=0.0f;
            painted_locations[writeIndex] = uvWorldPosition;
            
            Debug.Log(uvWorldPosition);
			return true;
		}
		else{		
			return false;
		}
		
	}
	//Sets the base material with a our canvas texture, then removes all our brushes
	void SaveTexture(){		
		brushCounter=0;
		System.DateTime date = System.DateTime.Now;
		RenderTexture.active = canvasTexture;
		Texture2D tex = new Texture2D(canvasTexture.width, canvasTexture.height, TextureFormat.RGB24, false);		
		tex.ReadPixels (new Rect (0, 0, canvasTexture.width, canvasTexture.height), 0, 0);
		tex.Apply ();
		RenderTexture.active = null;
		baseMaterial.mainTexture =tex;	//Put the painted texture as the base
		foreach (Transform child in brushContainer.transform) {//Clear brushes
			Destroy(child.gameObject);
		}
		//StartCoroutine ("SaveTextureToFile"); //Do you want to save the texture? This is your method!
		Invoke ("ShowCursor", 0.1f);
	}
	//Show again the user cursor (To avoid saving it to the texture)
	void ShowCursor(){	
		saving = false;
	}

	////////////////// PUBLIC METHODS //////////////////

	public void SetBrushMode(MyPainter_BrushMode brushMode){ //Sets if we are painting or placing decals
		mode = brushMode;
	//	brushCursor.GetComponent<SpriteRenderer> ().sprite = brushMode == MyPainter_BrushMode.PAINT ? cursorPaint : cursorDecal;
	}
	public void SetBrushSize(float newBrushSize){ //Sets the size of the cursor brush or decal
        brushSize = 0.1f;
		brushCursor.transform.localScale = Vector3.one * brushSize;
	}

	////////////////// OPTIONAL METHODS //////////////////

	#if !UNITY_WEBPLAYER 
		IEnumerator SaveTextureToFile(Texture2D savedTexture){		
			brushCounter=0;
			string fullPath=System.IO.Directory.GetCurrentDirectory()+"\\UserCanvas\\";
			System.DateTime date = System.DateTime.Now;
			string fileName = "CanvasTexture.png";
			if (!System.IO.Directory.Exists(fullPath))		
				System.IO.Directory.CreateDirectory(fullPath);
			var bytes = savedTexture.EncodeToPNG();
			System.IO.File.WriteAllBytes(fullPath+fileName, bytes);
			Debug.Log ("<color=orange>Saved Successfully!</color>"+fullPath+fileName);
			yield return null;
		}
	#endif
}
