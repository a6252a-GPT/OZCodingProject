using UnityEngine;

public class AudioSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            //객체가 없으면
            if (_instance == null)
            {   //현재씬에서 T타입을 찾는다
                _instance = FindFirstObjectByType<T>();
                //씬에도 없으면
                if(_instance == null)
                {   //새 게임오브젝트를 생성
                    GameObject obj = new GameObject($"_{typeof(T).Name}");
                    //T컴포넌트 추가
                    _instance = obj.AddComponent<T>();
                }
            }
            //찾거나 생성한 객체를 반환
            return _instance;
        }
    }
    //상속받은 클래스가 오버라이드가 가능하도록 virtual로 사용
    protected virtual void Awake()
    {
        //아직 싱글톤 객체가 없으면
        if(_instance == null)
        {
            //현재 객체를 싱글톤으로 등록
            //T타입으로 변환
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            if(_instance != this)
            {
                Destroy(gameObject);
            }
        }

    }
}
