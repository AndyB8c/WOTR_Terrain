using UnityEngine;
using System;


public class UnluckFlagGUI:MonoBehaviour{
    
    public GameObject[] prefabs;
    public Material[] bgrs;
    public Light[] lights;
    
    public GameObject nextButton;
    public GameObject prevButton;
    public GameObject bgrButton;
    public GameObject lightButton;
    
    public GameObject texturePreview;
    
    GameObject activeObj;
    int counter;
    int bCounter;
    int lCounter;
    public TextMesh txt;
    public TextMesh debug;
    
    public void Start() {
    	Swap ();
    	if(txt == null)
    	txt = transform.Find("txt").GetComponent<TextMesh>();
    	if(nextButton == null)
    	nextButton = transform.Find("nextButton").GetComponent<GameObject>();
    	if(prevButton == null)
    	prevButton = transform.Find("prevButton").GetComponent<GameObject>();
    	if(bgrButton == null)
    	bgrButton = transform.Find("bgrButton").GetComponent<GameObject>();
    	if(lightButton == null)
    	lightButton = transform.Find("lightButton").gameObject;
    	
    	
    	if(texturePreview == null)
    	texturePreview = transform.Find("texturePreview").GetComponent<GameObject>();
    	if(debug == null)
    	debug = transform.Find("debug").GetComponent<TextMesh>();
    	
    }
    
    public void Update() {
    	if(Input.GetMouseButtonUp(0)){		
    		ButtonUp();
    	}
    	if(Input.GetKeyUp("right"))
    	Next();
    	if(Input.GetKeyUp("left"))
    	Prev();
    	if(Input.GetKeyUp("space")){
    	nextButton.SetActive(!nextButton.activeInHierarchy);
    	prevButton.SetActive(nextButton.activeInHierarchy);
    	bgrButton.SetActive(nextButton.activeInHierarchy);
    	texturePreview.SetActive(nextButton.activeInHierarchy);
    	txt.gameObject.SetActive(nextButton.activeInHierarchy);
    	lightButton.gameObject.SetActive(nextButton.activeInHierarchy);
    	}
    }
    
    public void ButtonUp() {
    	Ray ray	= Camera.main.ScreenPointToRay(Input.mousePosition);
    	RaycastHit hit = new RaycastHit();
    	if (Physics.Raycast (ray, out hit)) {
    		if(hit.transform.gameObject == nextButton)
    			Next();	
    		else if(hit.transform.gameObject == prevButton)
    			Prev();	
    		else if(hit.transform.gameObject == bgrButton)
    			NextBgr();
    		else if(hit.transform.gameObject == lightButton)
    			LightChange();	
    	}
    }
    public void LightChange() {
    	if(lights.Length > 0){
    		lights[lCounter].enabled = false;
    		lCounter++;
    		if(lCounter >= lights.Length)
    		lCounter = 0;	
    		lights[lCounter].enabled = true;		
    	}
    }
    public void NextBgr() {
    	if(bgrs.Length > 0){
    		bCounter++;
    		if(bCounter >= bgrs.Length)
    		bCounter = 0;
    		RenderSettings.skybox = bgrs[bCounter];
    	}
    }
    
    public void Next() {	
    	counter++;
    	if(counter > prefabs.Length -1)
    		counter = 0;
    	Swap ();
    }
    
    public void Prev() {
    	counter--;
    	if(counter < 0)
    		counter = prefabs.Length -1;
    	Swap ();	
    }
    
    public void Swap() {
    	if(prefabs.Length > 0){
    		Destroy(activeObj);
    		GameObject o = (GameObject)Instantiate(prefabs[counter]);
    		activeObj = o;
    		if(txt != null){
    		txt.text = activeObj.name;
    		txt.text = txt.text.Replace("(Clone)", "");		
    		txt.text = txt.text + " " + activeObj.GetComponent<UnluckAnimatedMesh>().meshContainerFBX.name;
    		txt.text = txt.text.Replace("_", " ");
    		txt.text = txt.text.Replace("Flag ", "");
    		}
    		if(texturePreview != null)
    		texturePreview.GetComponent<Renderer>().sharedMaterial = activeObj.GetComponent<Renderer>().sharedMaterial;
    	}
    }
}
