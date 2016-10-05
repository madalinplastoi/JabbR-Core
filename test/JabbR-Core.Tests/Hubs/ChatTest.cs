﻿using Xunit;
using System;
using JabbR_Core.Hubs;
using JabbR_Core.ViewModels;
using System.Collections.Generic;
using JabbR_Core.Services;
using Microsoft.Extensions.Options;
using JabbR_Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections;

using Moq;
using Microsoft.AspNetCore.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using JabbR_Core.Controllers;
using Microsoft.AspNetCore.Builder.Internal;
using System.Threading.Tasks;
using System.Security.Claims;
using JabbR_Core.Infrastructure;
//using System.Dynamic;

namespace JabbR_Core.Tests.Hubs
{
    public class ChatTest
    {
        private Chat _chat;

        private ICache _cache;
        private JabbrContext _context;
        private ChatService _chatService;
        private InMemoryRepository _repository;
        private IRecentMessageCache _recentMessageCache;
        //private IOptions<ApplicationSettings> _settings;
        private OptionsManager<ApplicationSettings> _settings;

        public Action Convert(Action<HttpContext> action, HttpContext context)
        {
            if (action == null) return null;
            return new Action(() => { action(obj: context); });
        }

        public Task MyThing(HttpContext context)
        {
            HttpContext c;
            return new Task(() => c = context);
        }

        public ChatTest()
        {
            // Fetch new instances of the required objects
            GetCleanRepository();

            _cache = new DefaultCache();
            _recentMessageCache = new RecentMessageCache();
            _settings = new OptionsManager<ApplicationSettings>(new List<IConfigureOptions<ApplicationSettings>>() { });

            _chatService = new ChatService(_cache, _recentMessageCache, _repository);

            var request = new Mock<HttpRequest>();
            var connection = new Mock<IConnection>();
            var pipeline = new Mock<IHubPipelineInvoker>();
            var connectionId = "79482a87-8d16-42bc-b5ce-1fb7b309ad1e";

            var chat = new TestableChat(_repository, _settings, _chatService, connection);

            chat.Clients = new HubConnectionContext(pipeline.Object, chat.MockConnection.Object, "Chat", connectionId);

            
            // Include required claims with request for authentication
            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, "James"));
            claims.Add(new Claim(ClaimTypes.AuthenticationMethod, "provider"));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, "identity"));
            claims.Add(new Claim(ClaimTypes.Email, "james@no.com"));
            claims.Add(new Claim(JabbRClaimTypes.Identifier, "2"));

            var claimsIdentity = new ClaimsIdentity(claims, Constants.JabbRAuthType);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            Models.ChatUser user = new Models.ChatUser()
            {
                Name = "James",
                Email = "james@no.com",
                Id = "2",
                Identity = claimsIdentity.ToString()
            };

            _repository.Add(user);

            request.Setup(r => r.Cookies).Returns(new Mock<IRequestCookieCollection>().Object);
            request.Setup(r => r.HttpContext.User).Returns(claimsPrincipal);

            // Connection ID taken from actual JabbR-Core instance.
            chat.Context = new HubCallerContext(request.Object, connectionId);
            
            // Instantiate Chat hub.
            _chat = chat;
        }

        // Use this method at the beginning of tests to make sure that 
        // values in old tests won't impact the current one
        public void GetCleanRepository()
        {
            // Repository
            _context = new JabbrContext(new DbContextOptions<JabbrContext>());
            _repository = new InMemoryRepository(_context);
        }

        // Tests
        [Fact]
        public void GetRoomsNotNull()
        {
            Assert.NotEqual(null, _chat.GetRooms());
        }

        [Fact]
        public void GetCommandsNotNullNotEmpty()
        {
            Assert.NotEqual(null, _chat.GetCommands());
            Assert.NotEmpty((IEnumerable)_chat.GetCommands());
        }

        [Fact]
        public void GetShortcutsNotNullNotEmpty()
        {
            Assert.NotEqual(null, _chat.GetShortcuts());
            Assert.NotEmpty((IEnumerable)_chat.GetShortcuts());
        }

        // Context is null when called from external Hub, cannot test yet.
        [Fact]
        public void UpdateAcivityNoException()
        {
            _chat.UpdateActivity(testing: true);
        }

        // This throws an InvalidOperationException because actions that send through
        // SignalR hubs that are not instantiated in the SignalR pipeline are forbidden
        // Cannot test other operations until we change this behaviour.
        //[Fact]
        //public void LoadRoomsInvalidOperationException()
        //{
        //    GetCleanRepository(); 

        //    _repository.RoomList.Add(new Models.ChatRoom
        //    {
        //        Name = "ChatRoom One",
        //        Topic = "One"
        //    });

        //    // Need to reinstantiate _chat so that in its constructor the _repository.RoomList
        //    // assignment is up to date with the RoomList changes we added above.
        //    _chat = new Chat(_repository, _settings, _recentMessageCache, _chatService);

        //    Assert.Throws<InvalidOperationException>(() => _chat.LoadRooms());
        //}


        // Wouldn't this be better as a repository test?
        [Fact]
        public void AddRoomsVerification()
        {
            GetCleanRepository();

            var rooms = _chat.GetRooms();

            Assert.Empty(rooms);

            var room = new LobbyRoomViewModel()
            {
                Name = "Room",
                Topic = "JabbR"
            };

            rooms.Add(room);

            Assert.Contains(room, _chat.GetRooms());
        }

        [Fact]
        public void CreateRoomSendTrue()
        {
            Assert.True(_chat.Send("/create MyRoom", null, testing: true));
        }

    }

    public class TestableChat : Chat
    {
        public IJabbrRepository Repository { get; set; }
        public ChatService ChatService { get; set; }
        public Mock<IConnection> MockConnection { get; set; }
        public OptionsManager<ApplicationSettings> Settings { get; set; }
        //public OptionsManager<ApplicationSettings> Settings = new OptionsManager<ApplicationSettings>(new List<IConfigureOptions<ApplicationSettings>>() { });


        public TestableChat(IJabbrRepository repository, OptionsManager<ApplicationSettings> settings, ChatService chatService, Mock<IConnection> mockConnection) 
            : base(repository, settings, new Mock<RecentMessageCache>().Object, chatService)
        {
            Repository = repository;
            ChatService = chatService;
            MockConnection = mockConnection;
            Settings = settings;
        }
    }
}
