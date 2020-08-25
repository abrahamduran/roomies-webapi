using System;
using System.ComponentModel.DataAnnotations;
using Roomies.WebAPI.Models;

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

        public static implicit operator Roommate(CreateRoommate createRoommmate)
        {
            return new Roommate
            {
                Name = createRoommmate.Name,
                Email = createRoommmate.Email,
                Username = createRoommmate.Name.Substring(0, createRoommmate.Name.IndexOf(' ')).ToLower()
            };
        }
    }
}
