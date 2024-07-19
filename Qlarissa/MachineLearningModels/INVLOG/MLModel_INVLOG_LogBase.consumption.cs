﻿// This file was auto-generated by ML.NET Model Builder.
using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
namespace Qlarissa
{
    public partial class MLModel_INVLOG_LogBase
    {
        /// <summary>
        /// model input class for MLModel_INVLOG_LogBase.
        /// </summary>
        #region model input class
        public class ModelInput
        {
            [LoadColumn(0)]
            [ColumnName(@"RSquared")]
            public float RSquared { get; set; }

            [LoadColumn(1)]
            [ColumnName(@"SlopeOfOuterFunctionAtEndOfTrainingPeriod")]
            public float SlopeOfOuterFunctionAtEndOfTrainingPeriod { get; set; }

            [LoadColumn(2)]
            [ColumnName(@"TrainingPeriodDays")]
            public float TrainingPeriodDays { get; set; }

            [LoadColumn(3)]
            [ColumnName(@"P0")]
            public float P0 { get; set; }

            [LoadColumn(4)]
            [ColumnName(@"P1")]
            public float P1 { get; set; }

            [LoadColumn(5)]
            [ColumnName(@"P2")]
            public float P2 { get; set; }

            [LoadColumn(6)]
            [ColumnName(@"EstimateDeviationPercentage")]
            public float EstimateDeviationPercentage { get; set; }

        }

        #endregion

        /// <summary>
        /// model output class for MLModel_INVLOG_LogBase.
        /// </summary>
        #region model output class
        public class ModelOutput
        {
            [ColumnName(@"RSquared")]
            public float RSquared { get; set; }

            [ColumnName(@"SlopeOfOuterFunctionAtEndOfTrainingPeriod")]
            public float SlopeOfOuterFunctionAtEndOfTrainingPeriod { get; set; }

            [ColumnName(@"TrainingPeriodDays")]
            public float TrainingPeriodDays { get; set; }

            [ColumnName(@"P0")]
            public float P0 { get; set; }

            [ColumnName(@"P1")]
            public float P1 { get; set; }

            [ColumnName(@"P2")]
            public float P2 { get; set; }

            [ColumnName(@"EstimateDeviationPercentage")]
            public float EstimateDeviationPercentage { get; set; }

            [ColumnName(@"Features")]
            public float[] Features { get; set; }

            [ColumnName(@"Score")]
            public float Score { get; set; }

        }

        #endregion

        private static string MLNetModelPath = Path.GetFullPath("MachineLearningModels/INVLOG/MLModel_INVLOG_LogBase.mlnet");

        public static readonly Lazy<PredictionEngine<ModelInput, ModelOutput>> PredictEngine = new Lazy<PredictionEngine<ModelInput, ModelOutput>>(() => CreatePredictEngine(), true);


        private static PredictionEngine<ModelInput, ModelOutput> CreatePredictEngine()
        {
            var mlContext = new MLContext();
            ITransformer mlModel = mlContext.Model.Load(MLNetModelPath, out var _);
            return mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(mlModel);
        }

        /// <summary>
        /// Use this method to predict on <see cref="ModelInput"/>.
        /// </summary>
        /// <param name="input">model input.</param>
        /// <returns><seealso cref=" ModelOutput"/></returns>
        public static ModelOutput Predict(ModelInput input)
        {
            var predEngine = PredictEngine.Value;
            return predEngine.Predict(input);
        }
    }
}
