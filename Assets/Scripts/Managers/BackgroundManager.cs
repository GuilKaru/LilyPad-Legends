using System.Collections;
using UnityEngine;

public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager Instance { get; private set; }
    
    [Header("Background Layers")]
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private SpriteRenderer coliseum;
    [SerializeField] private SpriteRenderer mountains;
    [SerializeField] private SpriteRenderer mountains2;
    [SerializeField] private SpriteRenderer clouds;
    [SerializeField] private SpriteRenderer clouds2;

    [Header("Day/Evening/Night Sprites")]
    [SerializeField] private Sprite backgroundDay;
    [SerializeField] private Sprite backgroundEvening;
    [SerializeField] private Sprite backgroundNight;

    [SerializeField] private Sprite coliseumDay;
    [SerializeField] private Sprite coliseumEvening;
    [SerializeField] private Sprite coliseumNight;

    [SerializeField] private Sprite mountainsDay;
    [SerializeField] private Sprite mountainsEvening;
    [SerializeField] private Sprite mountainsNight;

    [SerializeField] private Sprite mountains2Day;
    [SerializeField] private Sprite mountains2Evening;
    [SerializeField] private Sprite mountains2Night;

    [SerializeField] private Sprite cloudsDay;
    [SerializeField] private Sprite cloudsEvening;
    [SerializeField] private Sprite cloudsNight;

    [SerializeField] private Sprite clouds2Day;
    [SerializeField] private Sprite clouds2Evening;
    [SerializeField] private Sprite clouds2Night;

    [Header("Cycle Settings")]
    [SerializeField] private float phaseDuration = 15f;
    [SerializeField] private float fadeDuration = 1f;

    public enum TimeOfDay { Day, Evening, Night }
    private TimeOfDay currentTime = TimeOfDay.Day;

    [Header("Cloud Movement")]
    [SerializeField] private float cloud1Speed = 2f;
    [SerializeField] private float cloud2Speed = 3f;
    [SerializeField] private float startX = -20f;
    [SerializeField] private float endX = 20f;

    [Header("Crowd Ambience")]
    [SerializeField] private AudioSource crowdSource;
    [SerializeField] private AudioClip crowdLoopClip;
    
    
    private bool hasStarted = false;
    private Coroutine dayNightCoroutine;
    
    private Vector3 cloudsStartPosition;
    private Vector3 clouds2StartPosition;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    
    
    private void Start()
    {
        cloudsStartPosition = clouds.transform.position;
        clouds2StartPosition = clouds2.transform.position;
        
        currentTime = TimeOfDay.Day;
        ApplySprites(TimeOfDay.Day);
    }

    private void Update()
    {
        if (!hasStarted) return;

        MoveCloud(clouds.transform, cloud1Speed);
        MoveCloud(clouds2.transform, cloud2Speed);
    }

    public void StartBackgroundSequence()
    {
        if (hasStarted) return;

        hasStarted = true;
        PlayCrowdAmbience();
        dayNightCoroutine = StartCoroutine(DayNightCycle());
    }

    private void MoveCloud(Transform cloud, float speed)
    {
        Vector3 pos = cloud.position;
        pos.x += speed * Time.deltaTime;

        if (pos.x >= endX)
            pos.x = startX;

        cloud.position = pos;
    }
    
    public void ChangeTo(TimeOfDay targetTime)
    {
        if (currentTime == targetTime)
            return;

        StopAllCoroutines();

        if (hasStarted)
            dayNightCoroutine = StartCoroutine(FadeToTime(targetTime));
        else
            StartCoroutine(FadeToTime(targetTime));

        currentTime = targetTime;
    }
    
    public void SetToDay()
    {
        ChangeTo(TimeOfDay.Day);
    }

    public void SetToEvening()
    {
        ChangeTo(TimeOfDay.Evening);
    }

    public void SetToNight()
    {
        ChangeTo(TimeOfDay.Night);
    }

    private IEnumerator DayNightCycle()
    {
        while (true)
        {
            yield return new WaitForSeconds(phaseDuration);

            TimeOfDay next = currentTime == TimeOfDay.Day ? TimeOfDay.Evening :
                TimeOfDay.Night;

            if (next == TimeOfDay.Night)
            {
                yield return StartCoroutine(FadeToTime(TimeOfDay.Night));
                currentTime = TimeOfDay.Night;
                yield break;
            }

            yield return StartCoroutine(FadeToTime(next));
            currentTime = next;
        }
    }

    private IEnumerator FadeToTime(TimeOfDay next)
    {
        SpriteRenderer newBackground = Instantiate(background, background.transform.parent);
        SpriteRenderer newColiseum = Instantiate(coliseum, coliseum.transform.parent);
        SpriteRenderer newMountains = Instantiate(mountains, mountains.transform.parent);
        SpriteRenderer newMountains2 = Instantiate(mountains2, mountains2.transform.parent);
        SpriteRenderer newClouds = Instantiate(clouds, clouds.transform.parent);
        SpriteRenderer newClouds2 = Instantiate(clouds2, clouds2.transform.parent);

        ApplySpritesToCustom(next, newBackground, newColiseum, newMountains, newMountains2, newClouds, newClouds2);

        newClouds.transform.position = clouds.transform.position;
        newClouds2.transform.position = clouds2.transform.position;

        SetAlpha(newBackground, 0f);
        SetAlpha(newColiseum, 0f);
        SetAlpha(newMountains, 0f);
        SetAlpha(newMountains2, 0f);
        SetAlpha(newClouds, 0f);
        SetAlpha(newClouds2, 0f);

        float timer = 0f;

        while (timer < fadeDuration)
        {
            float t = timer / fadeDuration;

            float oldA = Mathf.Lerp(1f, 0f, t);
            SetAlpha(background, oldA);
            SetAlpha(coliseum, oldA);
            SetAlpha(mountains, oldA);
            SetAlpha(mountains2, oldA);
            SetAlpha(clouds, oldA);
            SetAlpha(clouds2, oldA);

            float newA = Mathf.Lerp(0f, 1f, t);
            SetAlpha(newBackground, newA);
            SetAlpha(newColiseum, newA);
            SetAlpha(newMountains, newA);
            SetAlpha(newMountains2, newA);
            SetAlpha(newClouds, newA);
            SetAlpha(newClouds2, newA);

            MoveCloud(newClouds.transform, cloud1Speed);
            MoveCloud(newClouds2.transform, cloud2Speed);

            timer += Time.deltaTime;
            yield return null;
        }

        SetAlpha(background, 0f);
        SetAlpha(newBackground, 1f);

        ApplySprites(next);

        SetAlpha(background, 1f);
        SetAlpha(coliseum, 1f);
        SetAlpha(mountains, 1f);
        SetAlpha(mountains2, 1f);
        SetAlpha(clouds, 1f);
        SetAlpha(clouds2, 1f);

        Destroy(newBackground.gameObject);
        Destroy(newColiseum.gameObject);
        Destroy(newMountains.gameObject);
        Destroy(newMountains2.gameObject);
        Destroy(newClouds.gameObject);
        Destroy(newClouds2.gameObject);
    }

    private void SetAlpha(SpriteRenderer sr, float a)
    {
        Color c = sr.color;
        sr.color = new Color(c.r, c.g, c.b, a);
    }

    private void ApplySprites(TimeOfDay time)
    {
        switch (time)
        {
            case TimeOfDay.Day:
                background.sprite = backgroundDay;
                coliseum.sprite = coliseumDay;
                mountains.sprite = mountainsDay;
                mountains2.sprite = mountains2Day;
                clouds.sprite = cloudsDay;
                clouds2.sprite = clouds2Day;
                break;

            case TimeOfDay.Evening:
                background.sprite = backgroundEvening;
                coliseum.sprite = coliseumEvening;
                mountains.sprite = mountainsEvening;
                mountains2.sprite = mountains2Evening;
                clouds.sprite = cloudsEvening;
                clouds2.sprite = clouds2Evening;
                break;

            case TimeOfDay.Night:
                background.sprite = backgroundNight;
                coliseum.sprite = coliseumNight;
                mountains.sprite = mountainsNight;
                mountains2.sprite = mountains2Night;
                clouds.sprite = cloudsNight;
                clouds2.sprite = clouds2Night;
                break;
        }
    }

    private void ApplySpritesToCustom(TimeOfDay time,
        SpriteRenderer bg, SpriteRenderer col, SpriteRenderer m1, SpriteRenderer m2, SpriteRenderer c1, SpriteRenderer c2)
    {
        switch (time)
        {
            case TimeOfDay.Day:
                bg.sprite = backgroundDay;
                col.sprite = coliseumDay;
                m1.sprite = mountainsDay;
                m2.sprite = mountains2Day;
                c1.sprite = cloudsDay;
                c2.sprite = clouds2Day;
                break;

            case TimeOfDay.Evening:
                bg.sprite = backgroundEvening;
                col.sprite = coliseumEvening;
                m1.sprite = mountainsEvening;
                m2.sprite = mountains2Evening;
                c1.sprite = cloudsEvening;
                c2.sprite = clouds2Evening;
                break;

            case TimeOfDay.Night:
                bg.sprite = backgroundNight;
                col.sprite = coliseumNight;
                m1.sprite = mountainsNight;
                m2.sprite = mountains2Night;
                c1.sprite = cloudsNight;
                c2.sprite = clouds2Night;
                break;
        }
    }
    
    public void ResetBackground()
    {
        StopAllCoroutines();

        hasStarted = false;
        dayNightCoroutine = null;
        currentTime = TimeOfDay.Day;

        ApplySprites(TimeOfDay.Day);

        SetAlpha(background, 1f);
        SetAlpha(coliseum, 1f);
        SetAlpha(mountains, 1f);
        SetAlpha(mountains2, 1f);
        SetAlpha(clouds, 1f);
        SetAlpha(clouds2, 1f);

        if (clouds != null)
            clouds.transform.position = cloudsStartPosition;

        if (clouds2 != null)
            clouds2.transform.position = clouds2StartPosition;

        CleanupTemporarySprites();
    }
    
    private void CleanupTemporarySprites()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == background || sr == coliseum || sr == mountains || sr == mountains2 || sr == clouds || sr == clouds2)
                continue;

            Destroy(sr.gameObject);
        }
    }
    
    public void PlayCrowdAmbience()
    {
        if (crowdSource != null && crowdLoopClip != null)
            crowdSource.PlayOneShot(crowdLoopClip);
    }
    
    
    
}
    

