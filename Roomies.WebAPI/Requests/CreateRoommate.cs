using System;
using System.ComponentModel.DataAnnotations;
using Roomies.App.Models;

namespace Roomies.WebAPI.Requests
{
    public class CreateRoommate
    {
        [Required]
        [MaxLength(30)]
        public string Name { get; set; }
        [Required]
        [EmailAddress]
        [MaxLength(30)]
        public string Email { get; set; }

        public static implicit operator Roommate(CreateRoommate createRoommate)
        {
            return new Roommate
            {
                Name = createRoommate.Name,
                Email = createRoommate.Email,
                Username = createRoommate.Name.Substring(0, createRoommate.Name.IndexOf(' ')).ToLower()
            };
        }
    }
}
