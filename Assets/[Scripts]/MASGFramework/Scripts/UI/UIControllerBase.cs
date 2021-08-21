using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

namespace MoonAuSosiGi.UI
{
/*
 * UIComponentBase를 제어해 실제 컨텐츠 내용을 구현하는 클래스
 * UIPanelBase에 붙어 관리되거나, UIControllerBase에 붙어 관리되기도 함. 
 * 하위에 컨트롤러, 컴포넌트를 가질 수 있다.
 */
    [DisallowMultipleComponent]
    public abstract class UIControllerBase : MonoBehaviour
    {
        /// <summary>
        /// 부모 컨트롤러. 단 하나를 소유할 수 있으며 부모가 없는 경우도 존재
        /// </summary>
        [Header("Parent")] 
        [SerializeField] 
        UIControllerBase parentController = null;

        /// <summary>
        /// 이 컨트롤러가 제어하는 자식 컨트롤러 딕셔너리
        /// </summary>
        [Header("Controller")]
        private Dictionary<int, UIControllerBase> childControllerDic = new Dictionary<int, UIControllerBase>();

        /// <summary>
        /// 이 컨트롤러가 제어하는 컴포넌트 딕셔너리
        /// </summary>
        [Header("Component List")]
        private Dictionary<int, UIComponentBase> childComponentDic = new Dictionary<int, UIComponentBase>();
        
        // 여기서 데이터를 로드하고, 컴포넌트들을 초기화 해주는 형태로 구현
        public virtual void InitController()
        {
            HasCheckAndAddChildren();
            InitChildControllers();
            InitChildComponents();
        }

        public void DeleteController()
        {
            ClearChildControllers(true);
            ClearChildComponents(true);
        }

        // 소속 컴포넌트 들을 초기화 하는 등의 처리를 수행.
        public virtual void ClearController()
        {
            LogWarning($"{GetType().FullName} ClearComponent");
            ClearChildControllers();
            ClearChildComponents();
        }

        /// <summary>
        /// 컨트롤러의 액티브를 켜고 끌 수 있는 메서드. 기존과 달라야 수행된다.
        /// </summary>
        /// <param name="active"></param>
        /// <param name="isAllChildActive">하위 컨트롤러와 컴포넌트도 포함하는지</param>
        public virtual void SetActiveController(bool active, bool isAllChildActive = true)
        {
            if (active != gameObject.activeSelf)
            {
                if (isAllChildActive)
                {
                    SetActiveChildControllers(active);
                    SetActiveChildComponents(active);
                }

                gameObject.SetActive(active);

                if (active)
                    StartController();
                else
                    StopController();
            }
        }

        // active가 활성화 될 때 호출되는 메서드 
        public virtual void StartController()
        {
            foreach (var controllerKeyPair in childControllerDic)
            {
                controllerKeyPair.Value.StartController();
            }

            foreach (var componentKeyPair in childComponentDic)
            {
                componentKeyPair.Value.StartComponent();
            }
        }

        // active 비활성화 될 때 호출되는 메서드
        public virtual void StopController()
        {
            foreach (var controllerKeyPair in childControllerDic)
            {
                controllerKeyPair.Value.StopController();
            }

            foreach (var componentKeyPair in childComponentDic)
            {
                componentKeyPair.Value.StopComponent();
            }
        }


        /// <summary>
        /// 컨트롤러와 컴포넌트들을 등록한다.
        /// </summary>
        void HasCheckAndAddChildren()
        {
            // >> 자식 컨트롤러 부터 등록한다.
            {
                var controllerData = GetChildControllerData();
                SetupChildControllers(controllerData.controllerKeyArr, controllerData.controllerTypeArr);
            }
            // >> 자식 컴포넌트 등록
            {
                var componentData = GetChildComponentData();
                SetupChildComponents(componentData.componentKeyArr,componentData.componentTypeArr);
            }
        }

        /// <summary>
        /// 자식 컨트롤러들을 등록하기 위한 정보를 리턴해야 한다.
        /// Type에 등록되는 정보는 UIControllerBase를 상속받은 클래스의 타입이어야 함.
        /// </summary>
        /// <returns></returns>
        public abstract (int[] controllerKeyArr, Type[] controllerTypeArr) GetChildControllerData();
        /// <summary>
        /// 자식 컴포넌트들을 등록하기 위한 정보를 리턴해야한다.
        /// Type에 등록되는 정보는 UIComponentBase를 상속받은 클래스의 타입이어야 함.
        /// </summary>
        /// <returns></returns>
        public abstract (int[] componentKeyArr, Type[] componentTypeArr) GetChildComponentData();
        
        public void SetParentController(UIControllerBase parent)
        {
            parentController = parent;
        }

        public UIControllerBase GetParentController()
        {
            return parentController;
        }

        public T GetParentController<T>() where T : UIControllerBase
        {
            return parentController as T;
        }

        public void RefreshUI()
        {
            foreach (var controllerKeyPair in childControllerDic)
            {
                controllerKeyPair.Value.RefreshUI();
            }

            foreach (var componentKeyPair in childComponentDic)
            {
                componentKeyPair.Value.RefreshUI();
            }
        }
        
        protected void InitChildControllers()
        {
            foreach (var controllerKeyPair in childControllerDic)
            {
                var controller = controllerKeyPair.Value;
                controller.SetParentController(this);
                controller.InitController();
            }
        }

        protected void ClearChildControllers(bool resetChild = false)
        {
            
            foreach (var controllerKeyPair in childControllerDic)
            {
                var controller = controllerKeyPair.Value;
                controller.ClearChildControllers(resetChild);
                controller.ClearChildComponents(resetChild);
            }

            if (resetChild)
            {
                childControllerDic.Clear();
            }
        }

        void SetActiveChildControllers(bool active)
        {
            foreach (var controllerKeyPair in childControllerDic)
            {
                controllerKeyPair.Value.SetActiveController(active);
            }
        }

        protected void AddChildUIController(int key, UIControllerBase controller)
        {
            if (controller == null)
            {
                LogError("null인 컨트롤러를 추가할 수 없습니다.");
                return;
            }

            childControllerDic.Add(key, controller);
        }

        protected UIControllerBase GetChildUIController(int idx)
        {
            childControllerDic.TryGetValue(idx, out var controller);
            return controller;
        }

        protected T GetChildUIController<T>(int idx) where T : UIControllerBase
        {
            return GetChildUIController(idx) as T;
        }

        protected bool HasChildController(int idx)
        {
            return childControllerDic.ContainsKey(idx);
        }

        /// <summary>
        /// 하위 컨트롤러들을 가져와 세팅한다. 
        /// </summary>
        /// <param name="keyArray">키로 사용할 정수형 배열. 세팅할 컨트롤러 수 만큼 있어야 함</param>
        /// <param name="types">가져올 타입들. 키 배열 갯수만큼 주어져야 함</param>
        private void SetupChildControllers(int[] keyArray, Type[] types)
        {
            SetupChildControllersOrComponents(isChildControllers: true, keyArray, types);
        }

        /// <summary>
        /// 하위 컴포넌트들을 가져와 세팅한다. 
        /// </summary>
        /// <param name="keyArray">키로 사용할 정수형 배열. 세팅할 컨트롤러 수 만큼 있어야 함</param>
        /// <param name="types">가져올 타입들. 키 배열 갯수만큼 주어져야 함</param>
        private void SetupChildComponents(int[] keyArray, Type[] types)
        {
            SetupChildControllersOrComponents(isChildControllers: false, keyArray, types);
        }

        /// <summary>
        /// 컨트롤러와 컴포넌트를 가져와서 딕셔너리에 세팅
        /// </summary>
        /// <param name="isChildControllers">컨트롤러, 컴포넌트 구별용</param>
        /// <param name="keyArray">키로 사용할 정수 배열</param>
        /// <param name="types">가져올 타입</param>
        private void SetupChildControllersOrComponents(bool isChildControllers, int[] keyArray, Type[] types)
        {
            string tag = (isChildControllers ? "Controllers" : "Components");
            if (keyArray == null || types == null)
            {
                LogError($"SetupChild{tag} 실패. 대상이 null입니다.");
            }

            if (keyArray.Length != types.Length)
            {
                LogError($"SetupChild{tag} 실패. 매개변수의 갯수가 다릅니다.");
            }

            if (isChildControllers) childControllerDic.Clear();
            else                    childComponentDic.Clear();

            string failMessage = $"SetupChild{tag} Failed. UIControllerBase or UIComponentBase 타입이 아니거나, 찾을 수 없습니다.";
            for (int i = 0; i < keyArray.Length; i++)
            {
                Type currentType = types[i];
                int key = keyArray[i];

                if (isChildControllers)
                {
                    var childController = GetComponentInChildren(currentType, true) as UIControllerBase;

                    if (childController != null)
                    {
                        if (childControllerDic.ContainsKey(key))
                        {
                            LogError($"중복된 키가 있습니다. 해당 컨트롤러는 무시합니다. {childController.GetType().FullName}");
                        }
                        else
                        {
                            childControllerDic.Add(key, childController);
                        }
                    }
                    else
                    {
                        LogError(failMessage);
                    }
                }
                else
                {
                    var childComponent = GetComponentInChildren(currentType, true) as UIComponentBase;

                    if (childComponent != null)
                    {
                        if (childComponentDic.ContainsKey(key))
                        {
                            LogError($"중복된 키가 있습니다. 해당 컴포넌트는 무시합니다. {childComponent.GetType().FullName}");
                        }
                        else
                        {
                            childComponentDic.Add(key, childComponent);
                        }
                    }
                    else
                    {
                        LogError(failMessage);
                    }
                }
            }
        }

        protected void InitChildComponents()
        {
            foreach (var compKeyPair in childComponentDic)
            {
                compKeyPair.Value.InitComponent(this);
            }
        }

        protected void ClearChildComponents(bool reset = false)
        {
            foreach (var compKeyPair in childComponentDic)
            {
                compKeyPair.Value.ClearComponent();
            }

            if (reset)
            {
                childComponentDic.Clear();
            }
        }
        
        protected void SetActiveChildComponents(bool active)
        {
            foreach (var compKeyPair in childComponentDic)
            {
                compKeyPair.Value.SetComponentActive(active);
            }
        }

        /// <summary>
        /// 현재 컴포넌트 딕셔너리에 추가한다.
        /// </summary>
        /// <param name="component"></param>
        protected bool AddChildUIComponent(int key, UIComponentBase component)
        {
            if (component == null)
            {
                LogWarning("Component is null");
                return false;
            }

            if (childComponentDic.ContainsKey(key))
            {
                LogError($"해당 키에 이미 컴포넌트가 존재합니다. 삽입하려고 한 컴포넌트 : {component.GetType().FullName} 해당 키에 존재하는 컴포넌트 : {childComponentDic[key].GetType().FullName}");
                return false;
            }
            else
            {
                childComponentDic.Add(key, component);
                return true;
            }
        }

        /// <summary>
        /// 지정된 index에 해당하는 컴포넌트를 가져온다.
        /// </summary>
        /// <param name="key">가져울 키 </param>
        /// <returns></returns>
        protected UIComponentBase GetChildUIComponent(int key)
        {
            childComponentDic.TryGetValue(key, out var component);
            return component;
        }

        protected T GetChildUIComponent<T>(int idx) where T : UIComponentBase
        {
            return GetChildUIComponent(idx) as T;
        }
        
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

        public void PrintAllChildren()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var childKeyPair in childControllerDic)
            {
                if(childKeyPair.Value != null)
                    builder.Append($"Key {childKeyPair.Key} Controller {childKeyPair.Value.GetType().FullName}\n");
                else
                    builder.Append($"key {childKeyPair.Key} Controller is null!\n");
            }

            builder.Append("\n\n");
            foreach (var childKeyPair in childComponentDic)
            {
                if(childKeyPair.Value != null)
                    builder.Append($"Key {childKeyPair.Key} Component {childKeyPair.Value.GetType().FullName}\n");
                else
                    builder.Append($"key {childKeyPair.Key} Component is null!\n");
            }
        }
    }
}