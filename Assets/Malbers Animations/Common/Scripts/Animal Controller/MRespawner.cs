using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using MalbersAnimations.Scriptables;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace MalbersAnimations.Controller
{
    /// <summary>Use this Script's Transform as the Respawn Point</summary>
    public class MRespawner : MonoBehaviour
    {
        public static MRespawner instance;

        #region Respawn
        [Tooltip("Animal Prefab to Swpawn"), FormerlySerializedAs("playerPrefab")]
        public GameObject player;
        public StateID RespawnState;
        public FloatReference RespawnTime = new FloatReference(4f);
        [Tooltip("If True: it will destroy the MainPlayer GameObject and Respawn a new One")]
        public BoolReference DestroyAfterRespawn = new BoolReference(true);
       
        
        /// <summary>Active Player Animal GameObject</summary>
        private GameObject activePlayer;
        /// <summary>Active Player Animal</summary>
        private MAnimal activeAnimal;
        /// <summary>Old Player Animal GameObject</summary>
        private GameObject oldPlayer;

        #endregion

        public UnityEvent OnRestartGame = new UnityEvent();


        void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
        {
            OnRestartGame.Invoke();
            FindMainAnimal();
        }

        public virtual void SetPlayer(GameObject go) => player = go;

        void Start()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                gameObject.name = gameObject.name + " Instance";
                SceneManager.sceneLoaded += OnLevelFinishedLoading;
                FindMainAnimal();
            }
            else
            {
                Destroy(gameObject); //Destroy This GO since is already a Spawner in the scene
            }
        }
 

        public void ResetScene()
        {
            DestroyOldPlayer();

            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }

        /// <summary>Finds the Main Animal used as Player on the Active Scene</summary>
        void FindMainAnimal()
        {
            var animal = MAnimal.MainAnimal;

            if (animal)
            {
                activePlayer = animal.gameObject;                   //Set the Current Player
                activeAnimal = animal;                              //Set the Current Controller

                animal.OnStateChange.AddListener(OnCharacterDead);  //Listen to the Animal changes of states
                animal.TeleportRot(transform);                         //Move the Animal to is Start Position

                RespawnState =  RespawnState ?? animal.OverrideStartState;

                animal.OverrideStartState = RespawnState;
            }
            else if (activePlayer == null && player!= null)
                InstantiateNewPlayer();
        }

        /// <summary>Listen to the Animal States</summary>
        public void OnCharacterDead(int StateID)
        {
            if (StateID == StateEnum.Death)     //Means Death
            {
                oldPlayer = activePlayer;       //Store the old player IMPORTANT

                if (player!= null)
                    StartCoroutine(C_SpawnPrefab());
                else
                    Invoke(nameof(ResetScene), RespawnTime);
            }
        }

        public IEnumerator C_SpawnPrefab()
        {
            yield return new WaitForSeconds(RespawnTime);

            DestroyOldPlayer();

            yield return new WaitForEndOfFrame();

            InstantiateNewPlayer();
        }

        void DestroyOldPlayer()
        {
            if (oldPlayer != null)
            {
                if (DestroyAfterRespawn)
                    Destroy(oldPlayer);
                else
                    DestroyAllComponents(oldPlayer);
            }
        }

        void InstantiateNewPlayer()
        {
            activePlayer = Instantiate(player, transform.position, transform.rotation) as GameObject;
            activeAnimal = activePlayer.GetComponent<MAnimal>();
          
            activeAnimal.OverrideStartState = RespawnState;

            activeAnimal.OnStateChange.AddListener(OnCharacterDead);
            OnRestartGame.Invoke();
        }


        /// <summary>Destroy all the components on  Animal and leaves the mesh and bones</summary>
        private void DestroyAllComponents(GameObject target)
        {
            if (!target) return;

            var components = target.GetComponentsInChildren<MonoBehaviour>();

            foreach (var comp in components) Destroy(comp);
        
            var colliders = target.GetComponentsInChildren<Collider>();

            if (colliders != null)
            {
                foreach (var col in colliders) Destroy(col);
            }

            var rb = target.GetComponentInChildren<Rigidbody>();
            if (rb != null) Destroy(rb);
            var anim = target.GetComponentInChildren<Animator>();
            if (anim != null) Destroy(anim);
        }
    }
}