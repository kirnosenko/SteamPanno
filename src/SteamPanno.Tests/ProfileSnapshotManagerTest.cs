using System;
using System.Collections.Generic;
using SteamPanno.panno;
using Xunit;
using Shouldly;
using NSubstitute;
using System.Linq;

namespace SteamPanno
{
	public class ProfileSnapshotManagerTest
	{
		private readonly ProfileSnapshotManager manager;
		private readonly ProfileStorage storage;

		public ProfileSnapshotManagerTest()
		{
			storage = Substitute.For<ProfileStorage>();
			manager = new ProfileSnapshotManager(storage);
		}

		[Fact]
		public void ShouldReturnNothingWhenNoProfile()
		{
			storage.GetProfileData(
				Arg.Is<string>(x => x == "123"))
				.Returns((List<ProfileSnapshot>)null);

			manager.GetProfile("123").ShouldBeNull();
		}

		[Fact]
		public void ShouldReturnProfileAndKeepItInCache()
		{
			storage.GetProfileData(
				Arg.Is<string>(x => x == "123"))
				.Returns(new List<ProfileSnapshot>()
				{
					new ProfileSnapshot()
					{
						Timestamp = 100,
						Games = new PannoGame[]
						{
							new PannoGame()
							{
								Id = 10,
							}
						}
					}
				});

			var profile1 = manager.GetProfile("123");
			var profile2 = manager.GetProfile("123");

			profile2.ShouldBe(profile1);
			profile2.GetIncrementalSnapshots().First().Games.First().Id.ShouldBe(10);
			storage.ReceivedCalls().Count().ShouldBe(1);
		}

		[Fact]
		public void ShouldNotSaveProfileWhetItIsNotChanged()
		{
			storage.GetProfileData(
				Arg.Is<string>(x => x == "123"))
				.Returns(new List<ProfileSnapshot>()
				{
					new ProfileSnapshot()
					{
						Timestamp = 100,
						Games = new PannoGame[]
						{
							new PannoGame()
							{
								Id = 10,
								HoursOnRecord = 1000
							}
						}
					}
				});

			manager.UpdateProfile("123", new ProfileSnapshot()
			{
				Timestamp = 101,
				Games = new PannoGame[]
				{
					new PannoGame()
					{
						Id = 10,
						HoursOnRecord = 1000
					}
				}
			});

			storage.DidNotReceive().SaveProfile(
				Arg.Any<string>(),
				Arg.Any<ProfileSnapshot[]>());
			storage.ReceivedCalls().Count().ShouldBe(1);
		}

		[Fact]
		public void ShouldSaveProfileWhetItIsChanged()
		{
			storage.GetProfileData(
				Arg.Is<string>(x => x == "123"))
				.Returns(new List<ProfileSnapshot>()
				{
					new ProfileSnapshot()
					{
						Timestamp = 100,
						Games = new PannoGame[]
						{
							new PannoGame()
							{
								Id = 10,
								HoursOnRecord = 1000
							}
						}
					}
				});

			manager.UpdateProfile("123", new ProfileSnapshot()
			{
				Timestamp = 101,
				Games = new PannoGame[]
				{
					new PannoGame()
					{
						Id = 10,
						HoursOnRecord = 1001
					}
				}
			});

			storage.Received(1).SaveProfile(
				Arg.Any<string>(),
				Arg.Any<ProfileSnapshot[]>());
			storage.ReceivedCalls().Count().ShouldBe(2);
		}
	}
}
