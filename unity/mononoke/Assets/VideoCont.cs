using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using System.IO;


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
    public void Poll()
    {
        if (inDown)
        {
            time += Time.deltaTime;
        }
        isEdge = false;
        if (Input.GetKeyUp(KeyCode.Return))
        {
            isEdge = true;
            inDown = false;
        }
        if (Input.GetKeyDown(KeyCode.Return))
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
    private GameObject target;
    private AudioSource vacuumSound;
    private AudioSource cancelSound;
    private UnityEngine.UI.Slider level;
    MononokeData mononokeData = new MononokeData();

    Vector3 originalPos =new Vector3();
    // Use this for initialization
    void Start () {
        vacuumSound = GameObject.Find("Vacuum").GetComponent<AudioSource>();
        vacuumSound.Pause();
        cancelSound = GameObject.Find("Cancel").GetComponent<AudioSource>();
        cancelSound.Pause();

        mononokeData.Load();
        level = GameObject.Find("Level").GetComponent<UnityEngine.UI.Slider>();
        target = GameObject.Find("Target");
        target.SetActive(false);
        mononoke = GameObject.Find("Mononoke");
        mononoke.SetActive(false);
        originalPos=mononoke.transform.position;

        //video.renderMode = VideoRenderMode.MaterialOverride;
        video.renderMode = VideoRenderMode.CameraFarPlane;
        video.Play();
        video.Pause();
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
        float aspect = (float)minfo.tex.width / minfo.tex.height;
        mononoke.GetComponent<Renderer>().material.mainTexture = minfo.tex;
        Vector3 scale = mononoke.transform.localScale;
        scale.x = scale.y * aspect;
        mononoke.transform.localScale = scale;
        level.value = minfo.level;
        mononoke.SetActive(true);
        vacuumSound.Play();

    }
    void mononokeOff()
    {
        mononoke.SetActive(false);
        vacuumSound.Stop();

    }
    void mononokeShake(float time)
    {
        float z = 10f - 0.2f * time;
        if (z < 1) z = 1;
        //originalPos.z = z;
        mononoke.transform.position = originalPos + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
    }
    HitCount hitCount = new HitCount();
    // Update is called once per frame
    void Update() {
        Control();
        hitCount.Poll();

        if (mononokeId < 0)//No valid mononoke in this time
        {
            int m = mononokeData.FindStart((int)video.frame);//Here is New Mononoke
            if (0 <= m)
            {
                mononokeId = m;
                //show mononoke In target indicator
                MononokeInfo minfo = mononokeData.GetInfo(mononokeId);
                float aspect = (float)minfo.tex.width / minfo.tex.height;
                target.GetComponent<UnityEngine.UI.Image>().material.mainTexture = minfo.tex;
                Vector3 scale = target.transform.localScale;
                scale.x = scale.y * aspect;
                target.transform.localScale = scale;
                target.SetActive(true);
            }
        }
        else
        {
            if (mononokeData.IsEnd(mononokeId, (int)video.frame)) //Now disappering 
            {
                target.SetActive(false);
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
            if (hitCount.time > 3)//CAPTURED
            {
                Debug.Log("SUCESS TO GET");
                ///TODO:caputure effect
                hitCount.hitId = -1;
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
