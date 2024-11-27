namespace GameModule.Spinning_Wheel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using FeatureTemplate.Scripts.MonoUltils;
    using FeatureTemplate.Scripts.RewardHandle;
    using Game.Scripts.MVP;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameFoundation.Scripts.UIModule.Utilities.GameQueueAction;
    using GameFoundation.Scripts.Utilities.LogService;
    using UnityEngine;
    using Zenject;

    public interface IWeightable
    {
        float Weight { get; set; } 
    }
    
    public class WheelItem : IRewardRecord, IWeightable
    {
        public string RewardId    { get; set; }
        public string RewardType  { get; set; }
        public int    RewardValue { get; set; }
        public float  Weight      { get; set; }
    }
    
    public class WheelModel
    {
        private List<WheelItem> _items;

        public WheelModel(List<WheelItem> items)
        {
            _items = items;
        }

        public float GetTotalWeight()
        {
            return _items.Sum(item => item.Weight);
        }

        public List<float> GetItemProbabilities()
        {
            float totalWeight = GetTotalWeight();
            return _items.Select(item => item.Weight / totalWeight).ToList();
        }

        public WheelItem GetRandomItem()
        {
            float totalWeight      = GetTotalWeight();
            float randomValue      = UnityEngine.Random.Range(0, totalWeight);
            float cumulativeWeight = 0;

            foreach (var item in _items)
            {
                cumulativeWeight += item.Weight;
                if (randomValue <= cumulativeWeight)
                {
                    return item;
                }
            }

            return null;
        }

        public List<WheelItem> GetItems()
        {
            return _items;
        }
    }


    public class WheelView : BaseScreenViewTemplate
    {
        [SerializeField] public FeatureButtonView spinButton; // Nút quay
        [SerializeField] public Transform         wheelTransform;
        [SerializeField] public float             spinDuration = 3f; // Thời gian quay nón
    }

    public class WheelPresenter : BaseScreenPresenterTemplate<WheelView, WheelModel>
    {
        private WheelModel _model;
        

        public override UniTask BindData(WheelModel screenModel)
        {
            // Gắn callback cho sự kiện quay
            this.View.spinButton.InitButtonEvent(_ => this.HandleSpinRequested(), new FeatureButtonModel
            {
                ButtonName = "Spin",
                ScreenPresenter = this,
                ScreenViewName = this.View.name,
                ButtonStatus = ButtonStatus.On
            });
            
            return UniTask.CompletedTask;
        }

        private void HandleSpinRequested()
        {
            // Lấy item ngẫu nhiên từ model
            var selectedItem = _model.GetRandomItem();

            // Tính góc quay
            int   itemIndex    = _model.GetItems().IndexOf(selectedItem);
            float anglePerItem = 360f / _model.GetItems().Count;
            float targetAngle  = 360f * 3 + itemIndex * anglePerItem; // Quay 3 vòng và dừng tại item

            // Sử dụng DOTween để xoay bánh xe
            this.View.wheelTransform.DORotate(new Vector3(0, 0, -targetAngle), this.View.spinDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // Xử lý sau khi hoàn thành quay nón
                    Debug.Log($"Spin complete! Result: {selectedItem.RewardType} - Value: {selectedItem.RewardValue}");
            
                    // TODO: Thực hiện logic xử lý phần thưởng hoặc thông báo kết quả
                });
        }

        public WheelPresenter(SignalBus signalBus, GameQueueActionContext gameQueueActionContext, ScreenManager screenManager, SceneDirector sceneDirector, ILogService logger) : base(signalBus, gameQueueActionContext, screenManager, sceneDirector, logger)
        {
        }
    }


}