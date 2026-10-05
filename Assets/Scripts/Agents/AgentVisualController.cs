using System.Collections;
using UnityEngine;

public class AgentVisualController : MonoBehaviour
{
    //Fields
    #region Fields
        [Header("Animation References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Animator headAnimator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteRenderer headSpriteRenderer;
        [SerializeField] private SpriteRenderer eyesSpriteRenderer;
        [SerializeField] private SpriteRenderer mouthSpriteRenderer;
        [SerializeField] private SpriteRenderer hatSpriteRenderer;
        
        [SerializeField] private float flickerDuration = 0.4f;
        [SerializeField] private float flickerInterval = 0.08f;
        
        public AgentRunTime runtime;


        private Coroutine damageFlickerRoutine;
        private string currentState;


        private string idleAnim;
        private string attackAnim;
        private string chargeAnim;
        private string hurtAnim;
        private string deathAnim;
        
        private string headIdleAnim;
        private string headAttackAnim;
        private string headChargeAnim;
        
        private string bodyState;
        private string headState;


    #endregion

    //Initialization
    #region Initialization
        public void Initialize(string visualId, string headId, string eyesId, string mouthId, string hatId)
        {
            var controller = Resources.Load<RuntimeAnimatorController>($"Agents/{visualId}/{visualId}");


            if (controller == null)
            {
                Debug.LogError($"[ANIM ERROR] Could NOT load controller at: Agents/{visualId}/{visualId}");
            }
            else
            {
                animator.runtimeAnimatorController = controller;
                idleAnim = $"{visualId}_Idle";
                attackAnim = $"{visualId}_Attack";
                chargeAnim = $"{visualId}_Charge";
                hurtAnim = $"{visualId}_Hurt";
                deathAnim = $"{visualId}_Death";


                ChangeAnimationState(idleAnim);
            }
            
            headAttackAnim = "Head_Attack";
            headChargeAnim = "Head_Charge";
            headIdleAnim = "Head_Idle";
            
            if (runtime.headSpriteHolder)
            {
                Sprite headSprite = Resources.Load<Sprite>($"Heads/{headId}");
                if (headSprite != null) runtime.headSpriteHolder.GetComponent<SpriteRenderer>().sprite = headSprite;
                headSpriteRenderer = runtime.headSpriteHolder.GetComponent<SpriteRenderer>();
            }

            if (runtime.eyesSpriteHolder)
            {
                Sprite eyesSprite = Resources.Load<Sprite>($"Eyes/{eyesId}");
                if (eyesSprite != null) runtime.eyesSpriteHolder.GetComponent<SpriteRenderer>().sprite = eyesSprite;
                eyesSpriteRenderer = runtime.eyesSpriteHolder.GetComponent<SpriteRenderer>();
            }

            if (runtime.mouthSpriteHolder)
            {
                Sprite mouthSprite = Resources.Load<Sprite>($"Mouths/{mouthId}");
                if (mouthSprite != null) runtime.mouthSpriteHolder.GetComponent<SpriteRenderer>().sprite = mouthSprite;
                mouthSpriteRenderer = runtime.mouthSpriteHolder.GetComponent<SpriteRenderer>();
            }

            if (runtime.hatSpriteHolder)
            {
                Sprite hatSprite = Resources.Load<Sprite>($"Hats/{hatId}");
                if (hatSprite != null) runtime.hatSpriteHolder.GetComponent<SpriteRenderer>().sprite = hatSprite;
                hatSpriteRenderer = runtime.hatSpriteHolder.GetComponent<SpriteRenderer>();
            }
        }


    #endregion

    //Animation Controls
    #region Animation Controls


        public void PlayIdle()
        {
            ChangeAnimationState(idleAnim);
            ChangeHeadAnimationState(headIdleAnim);
        }

        public void PlayAttack()
        {
            ChangeAnimationState(attackAnim);
            ChangeHeadAnimationState(headAttackAnim);
        }

        public void PlayPreparingCharge()
        {
            ChangeAnimationState(chargeAnim);
            ChangeHeadAnimationState(headChargeAnim);
            runtime.PlayPreparingChargeVFX();
        }

        public void PlayCharge()
        {
            Debug.Log("Playing Charge Animation");
            ChangeAnimationState(chargeAnim);
            ChangeHeadAnimationState(headChargeAnim);
        } 
        public void PlayHurt() => ChangeAnimationState(hurtAnim);

        public void PlayDeath()
        {
            // stop low HP flicker if running
            StopLowHpFlicker();

            // Start fading
            StartCoroutine(DeathFadeRoutine(3f)); // tweak duration here
        }


        private void ChangeAnimationState(string newState)
        {
            if (currentState == newState) return;
            animator.Play(newState);
            //animator.Play(newState, 0, 0f); // reset to time 0
            currentState = newState;
        }
        
        private void ChangeHeadAnimationState(string newState)
        {
            if (currentState == newState) return;
            headAnimator.Play(newState);
            //headAnimator.Play(newState, 0, 0f);
            currentState = newState;
        }


        public float GetAttackAnimationLength()
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;


            foreach (var clip in clips)
            {
                if (clip.name.Contains("_Charge"))
                {
                    Debug.Log($"[ANIM LENGTH] Charge clip found: {clip.name} ({clip.length}s)");
                    return clip.length;
                }
            }


            return 0f;
        }
        
        
        


    #endregion
    
    //Damage Visuals
    #region Damage Visuals


        public void PlayDamageFlicker()
        {
            if (!spriteRenderer) return;
            if (damageFlickerRoutine != null) StopCoroutine(damageFlickerRoutine);
            damageFlickerRoutine = StartCoroutine(DamageFlickerRoutine());
        }


        private IEnumerator DamageFlickerRoutine()
        {
            float elapsed = 0f;
            while (elapsed < flickerDuration)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
                headSpriteRenderer.enabled = !headSpriteRenderer.enabled;
                eyesSpriteRenderer.enabled = !eyesSpriteRenderer.enabled;
                mouthSpriteRenderer.enabled = !mouthSpriteRenderer.enabled;
                hatSpriteRenderer.enabled = !hatSpriteRenderer.enabled;
                yield return new WaitForSeconds(flickerInterval);
                elapsed += flickerInterval;
            }
            spriteRenderer.enabled = true;
            headSpriteRenderer.enabled = true;
            eyesSpriteRenderer.enabled = true;
            mouthSpriteRenderer.enabled = true;
            hatSpriteRenderer.enabled = true;
            damageFlickerRoutine = null;
            
            // AFTER flicker → show buff/debuff visuals
            if (runtime && runtime.pendingEffects != null)
            {
                runtime.ApplyVisualEffects(runtime.pendingEffects);
                runtime.pendingEffects = null;
            }
        }
        
        
        private Coroutine lowHpFlickerRoutine;

        public void StartLowHpFlicker()
        {
            if (lowHpFlickerRoutine == null)
                lowHpFlickerRoutine = StartCoroutine(LowHpFlickerRoutine());
        }

        public void StopLowHpFlicker()
        {
            if (lowHpFlickerRoutine != null)
            {
                StopCoroutine(lowHpFlickerRoutine);
                lowHpFlickerRoutine = null;

                // Reset colors to normal
                SetAllColors(Color.white);
            }
        }

        private IEnumerator LowHpFlickerRoutine()
        {
            Color normal = Color.white;
            Color red = new Color(1f, 0.5f, 0.5f); // soft tint (adjust to taste)

            float speed = 1.5f; // how fast it pulses
            float t = 0f;

            while (true)
            {
                // PingPong goes 0→1→0 smoothly forever
                t += Time.deltaTime * speed;
                float lerp = Mathf.PingPong(t, 1f);

                Color current = Color.Lerp(normal, red, lerp);
                SetAllColors(current);

                yield return null; // Smooth every frame
            }
        }

        private void SetAllColors(Color c)
        {
            spriteRenderer.color = c;
            headSpriteRenderer.color = c;
            eyesSpriteRenderer.color = c;
            mouthSpriteRenderer.color = c;
            hatSpriteRenderer.color = c;
        }
        
        
        private IEnumerator DeathFadeRoutine(float duration = 1f)
        {
            // Get initial colors
            Color bodyColor = spriteRenderer.color;
            Color headColor = headSpriteRenderer.color;
            Color eyesColor = eyesSpriteRenderer.color;
            Color mouthColor = mouthSpriteRenderer.color;
            Color hatColor = hatSpriteRenderer.color;

            float t = 0f;

            while (t < duration)
            {
                float alpha = Mathf.Lerp(1f, 0f, t / duration);

                bodyColor.a = alpha;
                headColor.a = alpha;
                eyesColor.a = alpha;
                mouthColor.a = alpha;
                hatColor.a = alpha;

                spriteRenderer.color = bodyColor;
                headSpriteRenderer.color = headColor;
                eyesSpriteRenderer.color = eyesColor;
                mouthSpriteRenderer.color = mouthColor;
                hatSpriteRenderer.color = hatColor;

                t += Time.deltaTime;
                yield return null;
            }

            // Force alpha to zero at the end
            bodyColor.a = 0; spriteRenderer.color = bodyColor;
            headColor.a = 0; headSpriteRenderer.color = headColor;
            eyesColor.a = 0; eyesSpriteRenderer.color = eyesColor;
            mouthColor.a = 0; mouthSpriteRenderer.color = mouthColor;
            hatColor.a = 0; hatSpriteRenderer.color = hatColor;

            // Disable or destroy after fade
            runtime.gameObject.SetActive(false);
        }
  

    #endregion
    
    //Orientation
    #region Orientation

        public void FlipLeft() => transform.localScale = new Vector3(-1, 1, 1);
        public void FlipRight() => transform.localScale = new Vector3(1, 1, 1);
        
    #endregion
    
    //Frontend Build
    #region Frontend Build
    
    //Dynamically build the NFT from the frontend JSON traits
   public void ApplyNFTTraits(NFTTraits traits)
    {
        // Clean the input strings
        string safeSkin = !string.IsNullOrEmpty(traits.skin) ? traits.skin.Trim().Replace(" ", "_") : "";
        string safeHat = !string.IsNullOrEmpty(traits.hat) ? traits.hat.Trim().Replace(" ", "_") : "";
        string safeEyes = !string.IsNullOrEmpty(traits.eyes) ? traits.eyes.Trim().Replace(" ", "_") : "";
        string safeMouth = !string.IsNullOrEmpty(traits.mouth) ? traits.mouth.Trim().Replace(" ", "_") : "";
        
        Debug.Log($"[NFT Builder] Assembling NFT. SafeSkin: '{safeSkin}'");

        // HEAD (Driven by 'skin')
        if (headSpriteRenderer && !string.IsNullOrEmpty(safeSkin))
        {
            Sprite headSprite = Resources.Load<Sprite>($"Heads/{safeSkin}");
            if (headSprite) headSpriteRenderer.sprite = headSprite;
            else Debug.LogError($"[NFT Builder] ERROR: Missing Head! Looked exactly for: Resources/Heads/{safeSkin}");
        }

        // BODY (Driven by 'skin')
        if (spriteRenderer && !string.IsNullOrEmpty(safeSkin))
        {
            string bodyFileName = safeSkin;

            RuntimeAnimatorController bodySprite = Resources.Load<RuntimeAnimatorController>($"Agents/{bodyFileName}/{bodyFileName}");
            if (bodySprite) 
            {
                animator.runtimeAnimatorController = bodySprite;
                
                idleAnim = $"{bodyFileName}_Idle";
                attackAnim = $"{bodyFileName}_Attack";
                chargeAnim = $"{bodyFileName}_Charge";
                
                animator.Play(idleAnim, 0, 0f);
                headAnimator.Play(headIdleAnim, 0, 0f);

                bodyState = idleAnim;
                headState = headIdleAnim;

            }
            else 
            {
                Debug.LogError($"[NFT Builder] ERROR: Missing Body! Looked exactly for: Resources/DialogueBodies/{bodyFileName}/{bodyFileName}");
            }
        }

        // HATS (Driven by 'hat')
        if (hatSpriteRenderer && !string.IsNullOrEmpty(safeHat))
        {
            Sprite hatSprite = Resources.Load<Sprite>($"Hats/{safeHat}");
            if (hatSprite) 
            {
                hatSpriteRenderer.sprite = hatSprite;
                hatSpriteRenderer.gameObject.SetActive(true);
            }
            else 
            {
                Debug.LogError($"[NFT Builder] ERROR: Missing Hat! Looked exactly for: Resources/Hats/{safeHat}");
            }
        }

        // EYES
        if (eyesSpriteRenderer && !string.IsNullOrEmpty(safeEyes))
        {
            Sprite eyeSprite = Resources.Load<Sprite>($"Eyes/{safeEyes}");
            if (eyeSprite) eyesSpriteRenderer.sprite = eyeSprite;
            else Debug.LogError($"[NFT Builder] ERROR: Missing Eyes! Looked exactly for: Resources/Eyes/{safeEyes}");
        }

        // MOUTHS
        if (mouthSpriteRenderer && !string.IsNullOrEmpty(safeMouth))
        {
            Sprite mouthSprite = Resources.Load<Sprite>($"Mouths/{safeMouth}");
            if (mouthSprite) mouthSpriteRenderer.sprite = mouthSprite;
            else Debug.LogError($"[NFT Builder] ERROR: Missing Mouth! Looked exactly for: Resources/Mouths/{safeMouth}");
        }
    }
    #endregion
   
}
