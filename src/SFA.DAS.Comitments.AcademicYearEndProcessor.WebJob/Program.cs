﻿using System;
using SFA.DAS.Comitments.AcademicYearEndProcessor.WebJob.DependencyResolution;
using SFA.DAS.Comitments.AcademicYearEndProcessor.WebJob.Updater;
using SFA.DAS.NLog.Logger;

namespace SFA.DAS.Comitments.AcademicYearEndProcessor.WebJob
{
    class Program
    {
        static void Main()
        {
            var container = IoC.Initialize();

            var logger = container.GetInstance<ILog>();
            var updater = container.GetInstance<IAcademicYearEndExpiryProcessor>();

            logger.Info($"Starting {nameof(AcademicYearEndExpiryProcessor)}.WebJob");

            try
            {
                updater.RunUpdate().Wait();
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error running {nameof(AcademicYearEndExpiryProcessor)}.WebJob");
            }
        }
    }
}
