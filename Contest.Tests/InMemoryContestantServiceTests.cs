using System;
using System.Linq;
using Contest.Shared.Models;
using ContestServer.Services;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace Contest.Tests
{
    public class InMemoryContestantServiceTests
    {
        public Mock<IGameService> gameServiceMoq { get; private set; }

        private Mock<ITimeService> timeServiceMoq;
        private InMemoryContestantService contestantService;

        [SetUp]
        public void SetUp()
        {
            gameServiceMoq = new Mock<IGameService>();
            timeServiceMoq = new Mock<ITimeService>();
            contestantService = new InMemoryContestantService(gameServiceMoq.Object, timeServiceMoq.Object);
            timeServiceMoq.Setup(ts => ts.Now()).Returns(DateTime.Now);
        }

        [Test]
        public void CannotAddDuplicateNames()
        {
            var wednesday = new Contestant("wednesday", "some token", DateTime.Now, 0);
            var impostorWednesday = new Contestant("wednesday", "other token", DateTime.Now, 0);
            
            contestantService.AddContestant(wednesday);


            contestantService
                .Invoking(cs => cs.AddContestant(impostorWednesday))
                .Should()
                .Throw<ArgumentException>()
                .WithMessage("cannot add contestant with existing name");
        }

        [Test]
        public void ContestantWithCorrectBoardPasses()
        {
            var lastGeneration = 100;
            var finalBoard = new Coordinate[]
            {
                new Coordinate{ X = 1, Y = 1}
            };
            gameServiceMoq
                .Setup(gs => gs.GetNumGenerations())
                .Returns(lastGeneration);
            gameServiceMoq
                .Setup(gs => gs.CheckBoard(finalBoard))
                .Returns(true);

            var wednesday = new Contestant{
                Name = "wednesday",
                Token = "wednesday's token",
                StartedGameAt = DateTime.Now,
                GenerationsComputed = 0
            };
            contestantService.AddContestant(wednesday);

            wednesday.GenerationsComputed = lastGeneration;
            wednesday.FinalBoard = finalBoard;

            contestantService.UpdateContestant(wednesday);

            var newWednesday = contestantService.GetContestantByToken(wednesday.Token);
            newWednesday.CorrectFinalBoard.Should().BeTrue();
        }

        [Test]
        public void SubmitingFinalBoardAddsEndTimeToContestant()
        {
            var lastGeneration = 100;
            var finalBoard = new Coordinate[]
            {
                new Coordinate{ X = 1, Y = 1}
            };
            gameServiceMoq.Setup(gs => gs.GetNumGenerations()).Returns(lastGeneration);
            var wednesday = new Contestant{
                Name = "wednesday",
                Token = "wednesday's token",
                LastSeen = DateTime.Now,
                GenerationsComputed = 0,
                StartedGameAt = DateTime.Now
            };
            contestantService.AddContestant(wednesday);

            wednesday.GenerationsComputed = lastGeneration;
            wednesday.FinalBoard = finalBoard;

            contestantService.UpdateContestant(wednesday);

            wednesday = contestantService.GetContestantByToken(wednesday.Token);
            wednesday.Elapsed.Should().NotBeNull();
        }
    }
}