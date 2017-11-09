using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using System.IO;
using System.IO.Ports;


class MononokeInfo
{
    public int level;
    public string name;
    public Texture2D tex;

};
class MononokeData
{

    List<int> mononokeStart = new List<int>();
    List<int> mononokeEnd = new List<int>();
    private List<MononokeInfo> mononokeInfo = new List<MononokeInfo>();
    public void Load()
    {
        TextAsset csvFile = Resources.Load("data") as TextAsset;
        StringReader reader = new StringReader(csvFile.text);
        mononokeStart.Clear();
        mononokeEnd.Clear();
        mononokeInfo.Clear();

        while (reader.Peek() > -1)
        {
            string[] d = reader.ReadLine().Split(',');
            if (d.Length != 4) continue;
            MononokeInfo m = new MononokeInfo();
            mononokeStart.Add(int.Parse(d[0]));
            mononokeEnd.Add(int.Parse(d[1]));
            m.level = int.Parse(d[2]);
            m.name = d[3];
            m.tex = Resources.Load<Texture2D>(d[3]);
            mononokeInfo.Add(m);
        }
    }
    public int FindStart(int frame)
    {
        int i = mononokeStart.BinarySearch(frame);
        if (i < 0)
        {
            i = (~i) - 1; // search equal or less
            if (i < 0) return -1;
            if (IsEnd(i, frame)) return -1; //still valid now

        }
        return i;
    }
    public MononokeInfo GetInfo(int id)
    {
        return mononokeInfo[id];
    }
    public bool IsEnd(int mononokeId, int frame)
    {
        return (mononokeEnd[mononokeId] <= frame);
    }

};

class HitCount
{
    public float time = 0;
    bool inDown = false;
    bool isEdge = false;
    public int hitId = -1;
    public VideoCont videoCont;

    public void Poll()
    {
        if (inDown)
        {
            time += Time.deltaTime;
        }
        int k = videoCont.GetKey();
        isEdge = false;
        if (Input.GetKeyUp(KeyCode.Return) || k=='U')
        {
            isEdge = true;
            inDown = false;
        }
        if (Input.GetKeyDown(KeyCode.Return) || k=='D')
        {

            isEdge = true;
            inDown = true;
            time = 0;
        }
    }
    public bool IsDownEdge()
    {
        return inDown && isEdge;
    }
    public bool IsRiseEdge()
    {
        return (!inDown) && isEdge;
    }


}

public class VideoCont : MonoBehaviour {
    public VideoPlayer video;
    public UnityEngine.UI.Text text;
    private GameObject mononoke;
    private GameObject target,center;
    private AudioSource vacuumSound;
    private AudioSource cancelSound;
    private UnityEngine.UI.Slider level;
    MononokeData mononokeData = new MononokeData();
    private float captureLen = 3.5f;
    public string portName = "COM4";
    public int baudRate = 9600;
    private SerialPort serialPort_;
    private const int nofLed = 100;
    byte[] ledData = new byte[nofLed * 3+1];
    Texture2D[] centerImg = new Texture2D[3];
    Vector3 originalPos =new Vector3();
    HitCount hitCount = new HitCount();
    void SendLed()
    {
        if (serialPort_ == null) return;

        ledData[0] = (byte)'L';
        serialPort_.Write(ledData,0,nofLed*3+1);
    }
    void SetLed(int pos,byte r,byte g,byte b)
    {
        ledData[pos * 3 + 1] = r;
        ledData[pos * 3 + 2] = g;
        ledData[pos * 3 + 3] = b;
    }
    enum LedState { OFF,WAVE};
    LedState ledState = LedState.OFF;
    int ledCount = 0;
    void KickLed()
    {
        ledCount++;
        if (ledState == LedState.OFF)
        {
            for (int i = 0; i < nofLed; i++) SetLed(i, 0, 0, 0);

        }
        if (ledState == LedState.WAVE)
        {

            for (int i = 0; i < nofLed; i++) { 
                 SetLed(i, 
                        (byte)(((ledCount+i)%13 == 0) ? 255 : 0), 
                        (byte)(((ledCount+i)%15 == 0  )? 255:0    ), 
                        (byte)(((ledCount+i)%17 == 0) ? 255 : 0));
                }
        }
        SendLed();

    }


    public int GetKey()
    {
        if (serialPort_ == null) return -1;

        try
        {
            return serialPort_.ReadByte();
        }
        catch (System.Exception)
        {
            return -1;
        }
    }
    // Use this for initialization
    void Start () {
        try
        {
            serialPort_ = new SerialPort(portName, baudRate);
            serialPort_.Open();
            serialPort_.ReadTimeout = 1;
        }catch(System.Exception e)
        {
            serialPort_ = null;
        }
        hitCount.videoCont=this;

        vacuumSound = GameObject.Find("Vacuum").GetComponent<AudioSource>();
        vacuumSound.Pause();
        cancelSound = GameObject.Find("Cancel").GetComponent<AudioSource>();
        cancelSound.Pause();

        mononokeData.Load();
        level = GameObject.Find("Level").GetComponent<UnityEngine.UI.Slider>();
        target = GameObject.Find("Target");
        target.SetActive(false);
        center = GameObject.Find("Center");
        center.SetActive(false);
        centerImg[0] = Resources.Load<Texture2D>("s");
        centerImg[1] = Resources.Load<Texture2D>("m");
        centerImg[2] = Resources.Load<Texture2D>("l");


        mononoke = GameObject.Find("Mononoke");
        mononoke.SetActive(false);
        originalPos=mononoke.transform.position;

        //video.renderMode = VideoRenderMode.MaterialOverride;
        video.renderMode = VideoRenderMode.CameraFarPlane;
        video.Play();
        video.Pause();
    }

    void air()
    {
        if (serialPort_ == null) return;
        try
        {
            serialPort_.Write("o");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }
  

    private void Control()
    {
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (video.isPlaying)
            {
                video.Pause();
            }
            else
            {
                video.Play();
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }


        long delta = 0;
        if (Input.GetKeyDown(KeyCode.R)) {delta -= video.frame; }
        if (Input.GetKeyDown(KeyCode.RightArrow)) delta += 20;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) delta -= 20;
        if (Input.GetKeyDown(KeyCode.UpArrow)) delta += 1;
        if (Input.GetKeyDown(KeyCode.DownArrow)) delta -= 1;

        if (delta != 0)
        {
            long frame = video.frame;
            frame += delta;
            if (frame < 0) frame = 0;
            if ((long)video.frameCount < frame) frame = (long)video.frameCount;
            video.frame = frame;
        }

        text.text = video.frame.ToString() + "/" + video.frameCount.ToString();
    }
    int mononokeId = -1;
    private void mononokeOn()
    {
        MononokeInfo minfo = mononokeData.GetInfo(mononokeId);
        captureLen = minfo.level * 3.5f / 3;
        float aspect = (float)minfo.tex.width / minfo.tex.height;
        mononoke.GetComponent<Renderer>().material.mainTexture = minfo.tex;
        Vector3 scale = mononoke.transform.localScale;
        scale.x = scale.y * aspect;
        mononoke.transform.localScale = scale;
        level.value = minfo.level;
        mononoke.SetActive(true);
        ledState = LedState.WAVE;
        vacuumSound.Play();

    }
    void mononokeOff()
    {
        mononoke.SetActive(false);
        vacuumSound.Stop();
        ledState = LedState.OFF;
    }
    void mononokeShake(float time)
    {
        float z = 2.0f - 3.0f/captureLen * time;
        if (z < -0.1f) z = -0.1f;
        originalPos.z = z;
        mononoke.transform.position = originalPos + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
    }
    // Update is called once per frame
    void Update() {
        Control();
        hitCount.Poll();
        KickLed();
        if (mononokeId < 0)//No valid mononoke in this time
        {
            int m = mononokeData.FindStart((int)video.frame);//Here is New Mononoke
            if (0 <= m)
            {
                mononokeId = m;
                //show mononoke In target indicator
                MononokeInfo minfo = mononokeData.GetInfo(mononokeId);
                if (minfo == null)
                {
                    Debug.LogError("No Data at " + minfo.name);
                }
                float aspect = (float)minfo.tex.width / minfo.tex.height;
                target.GetComponent<UnityEngine.UI.Image>().material.mainTexture = minfo.tex;
                center.GetComponent<UnityEngine.UI.Image>().material.mainTexture = centerImg[minfo.level - 1];
                Debug.Log("LEVEL======" + minfo.level);
                Vector3 scale = target.transform.localScale;
                scale.x = scale.y * aspect;
                target.transform.localScale = scale;
                target.SetActive(true);
                center.SetActive(true);
            }
        }
        else
        {
            if (mononokeData.IsEnd(mononokeId, (int)video.frame)) //Now disappering 
            {
                target.SetActive(false);
                center.SetActive(false);
                level.value = 0;
                mononokeId = -1;
            }
            else // Mononoke in the House!
            {

                if (hitCount.IsDownEdge())
                {
                    Debug.Log("HIT!");

                    hitCount.hitId = mononokeId;
                    mononokeOn();
                }
            }
        }


        if (0 <= hitCount.hitId)//Mononoke is being capturerd (do Shake ) 
        {
            mononokeShake(hitCount.time);
            if (hitCount.time > captureLen)//CAPTURED
            {
                Debug.Log("SUCESS TO GET");
                ///TODO:caputure effect
                hitCount.hitId = -1;
                air();
                mononokeOff();
            }
            if (hitCount.IsRiseEdge())//CANCEL
            {
                Debug.Log("CANCEL BY USER");
                hitCount.hitId = -1;
                mononokeOff();
                cancelSound.Play(); 
            }
        }




    }
}
