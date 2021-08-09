using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using UnityEngine.Diagnostics;
using UnityEngine.Events;

/*
 * UI를 제어하는 컴포넌트. UIControllerBase에 의해 제어된다.
 * 일단 원칙은 UIControllerBase는 단 1개만 연결될 수 있다.
 * 타입별로 단 한번 등록이 가능 ( 주석 참고 )
 */
namespace MoonAuSosiGi.UI
{
    [DisallowMultipleComponent]
    public abstract class UIComponentBase : MonoBehaviour
    {
        // 바인딩된 UI가 들어갈 딕셔너리
        [SerializeField]
        Dictionary<Type, UnityEngine.Object[]> uiObjectDict = new Dictionary<Type, UnityEngine.Object[]>();
        
        UIControllerBase parentController = null;
        
        protected UIControllerBase GetController()
        {
            return parentController;
        }

        protected T GetController<T>() where T : UIControllerBase
        {
            return parentController as T;
        }
        
        /// <summary>
        /// 컴포넌트를 사용 가능한 상태로 만든다. 호출 뒤에 모든 UI들이 바인딩된다.
        /// 외부에서 데이터를 세팅하고 싶을 경우, 이후에 전달하는 형태가 권장.
        /// 이전에 전달할 수도 있지만 일관성을 위해 이후에 전달하는 것으로 함.
        /// </summary>
        /// <param name="parent"></param>
        public virtual void InitComponent(UIControllerBase parent)
        {
            LogNormal($"{this.GetType().FullName} InitComponent");

            //@todo 만약 컨트롤러에서 등록하지 않고 직접 따로 등록할 경우에 대한 처리가 필요 -> 누가 컨트롤하는지 모름 
            parentController = parent;

            uiObjectDict?.Clear();
            
            // >> 목적에 따라서 순서대로 호출.
            BindUI();
            LoadData();
            LoadUI();
            RefreshUI();
        }

        /// <summary>
        /// UI를 초기값으로 돌리거나, 데이터 정리를 위한 메서드
        /// </summary>
        public virtual void ClearComponent()
        {
            LogNormal($"{this.GetType().FullName} ClearComponent");
        }

        /// <summary>
        /// 컴포넌트들은 이 메서드로 액티브를 켜고 끌 수 있게 한다.
        /// </summary>
        /// <param name="active"></param>
        public virtual void SetComponentActive(bool active)
        {
            gameObject.SetActive(active);
        }

        /// <summary>
        /// 상위 컨트롤러가 Start되거나, Activate 되면 아래 컴포넌트들도 Start된다.
        /// 상위 컨트롤러가 시작되었을 때 호출되는 메서드
        /// </summary>
        public virtual void StartComponent()
        {
        }

        /// <summary>
        /// 상위 컨트롤러가 Stop되거나 Deactivate되면 아래 컴포넌트들도 Stop된다.
        /// 상위 컨트롤러가 중단되었을 때 호출되는 메서드 
        /// </summary>
        public virtual void StopComponent()
        {
        }
        
        /// <summary>
        /// Bind할 UI 들을 여기서 등록해야 함.
        /// </summary>
        protected abstract void BindUI();
        
        /// <summary>
        /// 테이블 데이터 등 데이터를 불러오는 곳을 알고 있을 경우, 여기서 처리하도록 함.
        /// </summary>
        protected virtual void LoadData()
        {

        }

        /// <summary>
        /// 테이블 데이터들도 불러와졌고, UI도 바인딩된 상태에서 최초로 수행해야할 로직을 담당.
        /// </summary>
        protected virtual void LoadUI()
        {

        }
        
        /// <summary>
        /// 데이터가 갱신될 경우 UI를 새로 세팅해주어야 하는데, 해당 행동을 여기서 처리.
        /// 상단 UIControllerBase에서도 호출이 가능하다.
        /// </summary>
        public virtual void RefreshUI()
        {

        }
        
        #region Bind UI Method

        // 이 컴포넌트의 GameObject 기준으로 Enum 이름을 바탕으로 UI를 검색한 뒤, 등록한다.
        // 이 메서드는 BindUI 메서드에서 호출하는 것을 권장
        /// <summary>
        /// 이 컴포넌트의 GameObject 기준으로 Enum 이름을 바탕으로 UI를 검색한 뒤, 등록
        /// BindUI 메서드에서 호출해야 한다.
        /// 동일 타입이 있을 경우 무시하므로, 최초에 모두 등록되도록 해야 한다.
        /// </summary>
        /// <param name="enumType">enum</param>
        /// <param name="recursive">재귀적으로 찾을지 여부</param>
        /// <typeparam name="T">UI Type</typeparam>
        protected void BindUIObjects<T>(Type enumType, bool recursive = true) where T : UnityEngine.Object
        {
            try
            {
                string[] uiNames = Enum.GetNames(enumType);
                UnityEngine.Object[] objects = new UnityEngine.Object[uiNames.Length];

                Type key = typeof(T);

                // @todo 동작 확인 필요
                if (uiObjectDict.ContainsKey(key))
                {
                    LogError($"동일 타입이 이미 등록되어 있습니다. {key.FullName}");
                    return;
                }
                
                uiObjectDict.Add(typeof(T), objects);


                for (int i = 0; i < uiNames.Length; i++)
                {
                    if (typeof(T) == typeof(GameObject))
                    {
                        objects[i] = Utils.FindChild(gameObject, uiNames[i], true);
                    }
                    else
                    {
                        objects[i] = Utils.FindChild<T>(gameObject, uiNames[i], true);
                    }
                }

            }
            catch (ArgumentException)
            {
                LogError($"{enumType.ToString()}이 EnumType이 아닙니다");
            }
        }
        
        /// <summary>
        /// 바인딩된 UI를 얻어온다.
        /// </summary>
        /// <param name="idx">처음 등록한 enum의 값을 int형으로 넣어주면 된다.</param>
        /// <typeparam name="T">받아올 타입</typeparam>
        /// <returns></returns>
        protected T Get<T>(int idx) where T : UnityEngine.Object
        {
            UnityEngine.Object[] objects;
            if (uiObjectDict.TryGetValue(typeof(T), out objects) == false)
                return null;

            if (objects == null)
                return null;

            if (idx < 0 || objects.Length <= idx)
                return null;

            return objects[idx] as T;
        }
        
        protected bool IsBindUIEmpty()
        {
            if (uiObjectDict != null)
                return uiObjectDict.Count > 0;
            return false;
        }

        protected void SetText(TextMeshPro textUI, string text, bool isLocalized, bool isShaodw = true)
        {
            if (textUI != null)
            {
                // @todo 동작 방식 확인 필요 
            }
        }

        protected Image GetImage(int idx)
        {
            return Get<Image>(idx);
        }
        
        protected Animation GetAnimation(int idx)
        {
            return Get<Animation>(idx);
        }

        protected GameObject GetGameObject(int idx)
        {
            return Get<GameObject>(idx);
        }

        protected TextMeshPro GetTextMeshPro(int idx)
        {
            return Get<TextMeshPro>(idx);
        }

        #endregion

        public void LogError(string errorMsg)
        {
            Debug.LogError(errorMsg);
        }

        public void LogWarning(string warningMsg)
        {
            Debug.LogWarning(warningMsg);
        }

        public void LogNormal(string normalMsg)
        {
            Debug.Log(normalMsg);
        }
    }

    interface ITabComponent
    {
        void Enter();
    }
}