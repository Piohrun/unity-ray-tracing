using UnityEngine;
using System.Collections.Generic;
public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader RayTracingShader;
    private RenderTexture _target;
    public Texture SkyboxTexture;
    public int howManySpheres;
    public int howManyBounces = 1;
    public Light DirectionalLight;

    private bool moveCamera = true;

    float mainSpeed = 10.0f; //regular speed
    float shiftAdd = 250.0f; //multiplied by how long shift is held.  Basically running
    float maxShift = 1000.0f; //Maximum speed when holdin gshift
    float camSens = 0.5f; //How sensitive it with mouse
    private Vector3 lastMouse = new Vector3(255, 255, 255); //kind of in the middle of the screen, rather than at the top (play)
    private float totalRun= 1.0f;

    private uint _currentSample = 0;
    private Material _addMaterial;

    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        List<Sphere> spheres = generateRandomSpheres(howManySpheres);
        ComputeBuffer sphereBuffer = new ComputeBuffer (spheres.Count,sizeof (float)*15);
        sphereBuffer.SetData(spheres);
        RayTracingShader.SetBuffer(0,"_Spheres", sphereBuffer);

        Debug.Log("Light position and direction");
        Debug.Log(DirectionalLight.transform.position);
        Debug.Log(DirectionalLight.transform.forward);
    }

    public struct Sphere {
        public Vector3 position;
        public float radius;
        public Vector3 color;
        public Vector3 reflectivity;
        public Vector3 orbitCenter;
        public float orbitRadius;
        public float orbitAngle;
        //public float posX;
	    //public float posY;
	    //public float posZ;

        public void randomSphere(){

            radius = Random.Range(1.0f,5.0f);
            float posX = Random.Range(-15.0f,15.0f);
            float posY = Random.Range( 0.0f,15.0f) + radius;
            float posZ = Random.Range(-5.0f,5.0f);
            position = new Vector3(posX,posY,posZ);
            orbitCenter = new Vector3(posX+radius+Random.Range(0.0f,5.0f) ,posY,posZ+radius+Random.Range(0.0f,5.0f));
            orbitRadius = Random.Range(1.0f,15.0f);
            orbitAngle = 0.0f;

            color = new Vector3(Random.Range(0.0f,1.0f),Random.Range(0.0f,1.0f),Random.Range(0.0f,1.0f));
            float albedo = Random.Range(0.0f,12.0f);
            reflectivity = new Vector3(albedo,albedo,albedo);
            Debug.Log(position);
            Debug.Log(radius);
        }

        public void move(Vector3 direction){
            position += direction;
        }

    }


    public List<Sphere> generateRandomSpheres(int howMany)
        {
        
        List<Sphere> spheres = new List<Sphere>();

        for (int i = 0; i < howMany; i++)
            {
            Sphere s = new Sphere();
            s.randomSphere();
            spheres.Add(s);
            }
        return spheres;

        }


    private void SetShaderParameters()
    {
        RayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        RayTracingShader.SetInt("_Bounces",howManyBounces);
        Vector3 l = DirectionalLight.transform.forward;
        //Vector3 l = DirectionalLight.transform.position;
        RayTracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, DirectionalLight.intensity));
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
    }

    private void Render(RenderTexture destination)
    {
        // Make sure we have a current render target
        InitRenderTexture();
        // Set the target and dispatch the compute shader
        RayTracingShader.SetTexture(0, "Result", _target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 0.5f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 0.5f);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        // Blit the result texture to the screen
        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        _addMaterial.SetFloat("_Sample", _currentSample);
        Graphics.Blit(_target, destination, _addMaterial);
        _currentSample++;
    }
    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            // Release render texture if we already have one
            if (_target != null)
                _target.Release();

            // Get a render target for Ray Tracing
            _currentSample = 0;
            _target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();
        }
    }

    private Vector3 GetBaseInput() { //returns the basic values, if it's 0 than it's not active.
        Vector3 p_Velocity = new Vector3();
        if (Input.GetKey (KeyCode.W)){
            p_Velocity += new Vector3(0, 0 , 1);
        }
        if (Input.GetKey (KeyCode.S)){
            p_Velocity += new Vector3(0, 0, -1);
        }
        if (Input.GetKey (KeyCode.A)){
            p_Velocity += new Vector3(-1, 0, 0);
        }
        if (Input.GetKey (KeyCode.D)){
            p_Velocity += new Vector3(1, 0, 0);
        }
                                          
        return p_Velocity;
    }

    private void Update()
    {
    if (transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
        }
       
    if (DirectionalLight.transform.hasChanged){

        _currentSample = 0;
        DirectionalLight.transform.hasChanged = false;

        }


        if (Input.GetKey (KeyCode.Space)){
            moveCamera = !moveCamera;
        } 

        Vector3 p = GetBaseInput();

        if (moveCamera){

        lastMouse = Input.mousePosition - lastMouse ;
        lastMouse = new Vector3(-lastMouse.y * camSens, lastMouse.x * camSens, 0 );
        lastMouse = new Vector3(transform.eulerAngles.x + lastMouse.x , transform.eulerAngles.y + lastMouse.y, 0);
        transform.eulerAngles = lastMouse;
        lastMouse =  Input.mousePosition;
        //Mouse  camera angle done.  
       
        //Keyboard commands
        //float f = 0.0f;
        
        if (Input.GetKey (KeyCode.LeftShift)){
            totalRun += Time.deltaTime;
            p  = p * totalRun * shiftAdd;
            p.x = Mathf.Clamp(p.x, -maxShift, maxShift);
            p.y = Mathf.Clamp(p.y, -maxShift, maxShift);
            p.z = Mathf.Clamp(p.z, -maxShift, maxShift);
        }
        else{
            totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
            p = p * mainSpeed;
        }
       
        p = p * Time.deltaTime;
       Vector3 newPosition = transform.position;
        if (Input.GetKey(KeyCode.Space)){ //If player wants to move on X and Z axis only
            transform.Translate(p);
            newPosition.x = transform.position.x;
            newPosition.z = transform.position.z;
            transform.position = newPosition;
        }
        else{
            transform.Translate(p);
        }
        }
    }

    
}