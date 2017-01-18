using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InfrastructureServices.Pendulum.Domain.ApplicationServices.ScheduleRunner;
using InfrastructureServices.Pendulum.Domain.Data.Repository.Models;
using InfrastructureServices.Pendulum.Domain.Data.Repository.Stores;
using InfrastructureServices.Pendulum.Domain.Data.Repository.TestData;
using Moq;
using Moq.Protected;
using Xunit;


namespace InfrastructureServices.Pendulum.Domain.ApplicationServices.Tests.SchedulerRunnerX
{
    /// <summary>
    /// This class is responsible for testing the schedule runner service
    /// </summary>
    public class ScheduleRunnerServiceFacts
    {
        /// <summary>
        /// This class is responsible for testing how the schedule runner accesses the 
        /// schedule store's data. This class will test how many times a schedule domain 
        /// model should be updated based on its property values. It will also perform data validation.
        /// </summary>
        [Trait("Category", "Unit Tests")]
        [Trait("Category", "Schedule Runner: Schedule Store Access")]
        public class ScheduleStoreAccessFacts : IDisposable
        {
            private Mock<IScheduleStore> _mockScheduleStore;
            private Mock<IScheduleHistoryStore> _mockScheduleHistoryStore;
            private ScheduleRunnerService _scheduleRunnerService;
            private Schedule _schedulePriorToUpdate;

            public ScheduleStoreAccessFacts()
            {
                #region Setup

                _mockScheduleStore = new Mock<IScheduleStore>();
                _mockScheduleStore.Setup(s => s.UpdateAsync(It.IsAny<Schedule>()))
                    .Callback<Schedule>(sch => _schedulePriorToUpdate = sch)
                    .Returns(Task.FromResult(It.IsAny<Schedule>()));

                _mockScheduleHistoryStore = new Mock<IScheduleHistoryStore>();

                var handler = new Mock<HttpMessageHandler>();
                handler.Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .Returns(Task<HttpResponseMessage>.Factory.StartNew(() => new HttpResponseMessage(HttpStatusCode.OK)));

                var mockHttpClient = new HttpClient(handler.Object);

                _scheduleRunnerService = new ScheduleRunnerService(_mockScheduleStore.Object, _mockScheduleHistoryStore.Object, mockHttpClient);

                #endregion
            }

            public void Dispose()
            {
                _mockScheduleStore = null;
                _scheduleRunnerService = null;
                _schedulePriorToUpdate = null;
                _mockScheduleHistoryStore = null;
            }

            #region Valid Schedule Facts

            [Fact(DisplayName = "When running a valid schedule, the schedule store should only update the schedule once")]            
            public void WhenRunningAValidSchedule_ValidSchedule_ShouldBeUpdatedOnce()
            {
                //Act
                Task.WaitAll(_scheduleRunnerService.RunActiveSchedule(ScheduleData.ValidSchedule));

                //Assert
                _mockScheduleStore.Verify(s => s.UpdateAsync(It.IsAny<Schedule>()), Times.Once());
            }

            [Fact(DisplayName = "When running a valid schedule, the schedule store should update the schedule's last ran date to today")]
            public void WhenRunningAValidSchedule_ValidSchedule_LastRanDateShouldBeUpdated()
            {
                //Act
                Task.WaitAll(_scheduleRunnerService.RunActiveSchedule(ScheduleData.ValidSchedule));

                //Assert
                _schedulePriorToUpdate.LastRanDate.Day.Should().Be(DateTime.Now.Day);
            }
            
            #endregion

            #region Inactive Schedule Facts

            [Fact(DisplayName = "When running an inactive schedule, the schedule store should not update the schedule")]
            public void WhenRunningAnInActiveSchedule_InActiveSchedule_ShouldNotBeUpdated()
            {
                //Act
                Task.WaitAll(_scheduleRunnerService.RunActiveSchedule(ScheduleData.InActiveSchedule));

                //Assert
                _mockScheduleStore.Verify(s => s.UpdateAsync(It.IsAny<Schedule>()), Times.Never);
            }

            #endregion

            #region Expired Schedule Facts

            [Fact(DisplayName = "When running an expired schedule, the schedule store should only update the schedule once")]
            public void WhenRunningAnExpiredSchedule_ExpiredSchedule_ShouldBeUpdatedOnce()
            {
                //Act
                Task.WaitAll(_scheduleRunnerService.RunActiveSchedule(ScheduleData.ExpiredSchedule));

                //Assert
                _mockScheduleStore.Verify(s => s.UpdateAsync(It.IsAny<Schedule>()), Times.Once());
            }

            [Fact(DisplayName = "When running an expired schedule, the schedule store should update the schedule status to false")]
            public void WhenRunningAnExpiredSchedule_ExpiredSchedule_ShouldBeDeActivated()
            {
                //Act
                Task.WaitAll(_scheduleRunnerService.RunActiveSchedule(ScheduleData.ExpiredSchedule));

                //Assert
                _schedulePriorToUpdate.Status.Should().Be(false);
            }

            #endregion
        }

        /// <summary>
        /// This class is responsible for testing how the schedule runner accesses the 
        /// schedule history store's data. This class will test how many times a schedule's history
        /// is recorded based on the schedule's property values. It will also perform data validation.
        /// </summary>
        [Trait("Category", "Unit Tests")]
        [Trait("Category", "Schedule Runner: Schedule History Store Access")]
        public class ScheduleHistoryStoreAccessFacts : IDisposable
        {
            private Mock<IScheduleHistoryStore> _mockScheduleHistoryStore;
            private Mock<IScheduleStore> _mockScheduleStore;
            private ScheduleRunnerService _scheduleRunnerService;
            private ScheduleHistory _scheduleHistoryPriorToCreate;

            public ScheduleHistoryStoreAccessFacts()
            {
                #region Setup

                _mockScheduleHistoryStore = new Mock<IScheduleHistoryStore>();
                _mockScheduleHistoryStore.Setup(s => s.CreateAsync(It.IsAny<ScheduleHistory>()))
                    .Callback<ScheduleHistory>(sch => _scheduleHistoryPriorToCreate = sch)
                    .Returns(Task.FromResult(It.IsAny<ScheduleHistory>()));

                var handler = new Mock<HttpMessageHandler>();
                handler.Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .Returns(Task<HttpResponseMessage>.Factory.StartNew(() => new HttpResponseMessage(HttpStatusCode.OK)));

                var mockHttpClient = new HttpClient(handler.Object);

                _mockScheduleStore = new Mock<IScheduleStore>();

                _scheduleRunnerService =
                    new ScheduleRunnerService(_mockScheduleStore.Object, _mockScheduleHistoryStore.Object, mockHttpClient);

                #endregion
            }

            public void Dispose()
            {
                _mockScheduleStore = null;
                _mockScheduleHistoryStore = null;
                _scheduleRunnerService = null;
                _scheduleHistoryPriorToCreate = null;
            }

            #region Valid Schedule Facts
            
            [Fact(DisplayName = "When running a valid schedule, the schedule should be created in the schedule history")]
            public void WhenRunningAValidSchedule_ValidSchedule_ShouldBeCreatedInScheduleHistory()
            {
                //Act
                Task.WaitAll(_scheduleRunnerService.RunActiveSchedule(ScheduleData.ValidSchedule));

                //Assert
                _mockScheduleHistoryStore.Verify(sh => sh.CreateAsync(It.IsAny<ScheduleHistory>()), Times.Once());
            }

            [Fact(DisplayName = "When running a valid schedule, the schedule history should have the response status 200")]
            public void WhenRunningAValidScheudle_ScheduleHistory_ShouldHaveAResponseStatus200()
            {
                //Act
                Task.WaitAll(_scheduleRunnerService.RunActiveSchedule(ScheduleData.ValidSchedule));

                //Assert
                _scheduleHistoryPriorToCreate.ResponseStatus.Should().Be(200);
            }

            #endregion

            #region Inactive Schedule Facts

            [Fact(DisplayName = "When running an inactive schedule, the schedule should not be created in the schedule history")]
            public void WhenRunningAnInActiveSchedule_InActiveSchedule_ShouldNotBeCreatedInScheduleHistory()
            {
                //Act
                Task.WaitAll(_scheduleRunnerService.RunActiveSchedule(ScheduleData.InActiveSchedule));

                //Assert
                _mockScheduleHistoryStore.Verify(sh => sh.CreateAsync(It.IsAny<ScheduleHistory>()), Times.Never());
            }

            #endregion

            #region Expired Schedule Facts

            [Fact(DisplayName = "When running an expired schedule, the schedule should not be created in the schedule history")]
            public void WhenRunningAnExpiredSchedule_ExpiredSchedule_ShouldNotBeCreatedInScheduleHistory()
            {
                //Act
                Task.WaitAll(_scheduleRunnerService.RunActiveSchedule(ScheduleData.ExpiredSchedule));

                //Assert
                _mockScheduleHistoryStore.Verify(sh => sh.CreateAsync(It.IsAny<ScheduleHistory>()), Times.Never());
            }

            #endregion
        }
    }
}
