using System.Collections.Generic;
using Auki.Ur;
using UnityEngine;
using UnityEngine.UI;

namespace AukiHandTrackerSample
{
    public class HandCalibration : MonoBehaviour
    {
        // PlayerPrefs keys to save calibration result for future use
        private const string HandSizeKey = "Ur.Sample.HandSize"; 
        private const string ZScaleKey = "Ur.Sample.ZScale";
        
        [SerializeField] private Text calibrationText;
        [SerializeField] private Button calibrationButton;
        [SerializeField] private Image calibrationStatusImage;

        private HandTracker _handTracker;

        private Dictionary<HandTracker.CalibrationStatus, Color> _calibrationStatusColors =
            new Dictionary<HandTracker.CalibrationStatus, Color>()
            {
                { HandTracker.CalibrationStatus.CALIBRATED, Color.green },
                { HandTracker.CalibrationStatus.CALIBRATING, Color.clear },
                { HandTracker.CalibrationStatus.NOT_CALIBRATED, Color.red },
                { HandTracker.CalibrationStatus.MANUAL_CALIBRATION, Color.cyan }
            };

        private void Start()
        {
            _handTracker = HandTracker.GetInstance();
            LoadCalibrationState();
            
            calibrationButton.onClick.AddListener(CalibrateHandTracker);
        }

        /// <summary>
        /// Save calibration parameters in PlayerPrefs to avoid asking the same user to calibrate again in the future
        /// </summary>
        private void SaveCalibrationState()
        {
            var state = _handTracker.GetCalibrationState();
            PlayerPrefs.SetFloat(HandSizeKey, state.HandSize);
            PlayerPrefs.SetFloat(ZScaleKey, state.ZScale);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Set calibration state manually if a previous one was saved in PlayerPrefs
        /// </summary>
        private void LoadCalibrationState()
        {
            if (PlayerPrefs.HasKey(HandSizeKey))
            {
                _handTracker.SetCalibrateState(new HandTracker.CalibrationState()
                {
                    HandSize = PlayerPrefs.GetFloat(HandSizeKey),
                    ZScale = PlayerPrefs.GetFloat(ZScaleKey)
                });
            }
            
            calibrationStatusImage.color = _calibrationStatusColors[_handTracker.GetCalibrationStatus()];
        }

        /// <summary>
        /// Start the calibration process
        /// The user should place their hand on a flat surface plane (like a table)
        /// During the the process calibration progress will be displayed.
        /// When finished the status text and image will be updated 
        /// </summary>
        public void CalibrateHandTracker()
        {
            calibrationButton.interactable = false;
            _handTracker.StartCalibration(report =>
            {
                calibrationStatusImage.color = _calibrationStatusColors[_handTracker.GetCalibrationStatus()];
                switch (report.StatusReport)
                {
                    case HandTracker.CalibrationStatusReport.CALIBRATION_FINISHED:
                    {
                        calibrationButton.interactable = true;
                        calibrationText.text = "Calibrated";
                        SaveCalibrationState();
                        break;
                    }
                    case HandTracker.CalibrationStatusReport.CALIBRATION_PROGRESS:
                    {
                        calibrationText.text = report.Progress.ToString("P");
                        break;
                    }
                    case HandTracker.CalibrationStatusReport.FAILURE_NO_HAND:
                    case HandTracker.CalibrationStatusReport.FAILURE_NO_PLANE:
                    case HandTracker.CalibrationStatusReport.FAILURE_NO_MEASUREMENTS:
                    {
                        calibrationButton.interactable = true;
                        calibrationText.text = "Try again";
                        break;
                    }
                    case HandTracker.CalibrationStatusReport.CALIBRATION_AR_NOT_READY:
                    {
                        calibrationButton.interactable = true;
                        calibrationText.text = "Scan the room";
                        break;
                    }
                }
            }, false);
        }
    }
}