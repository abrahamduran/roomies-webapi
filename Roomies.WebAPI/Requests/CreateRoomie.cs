using System;
using System.ComponentModel.DataAnnotations;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Requests
{
    public class CreateRoomie
    {
        [Required]
        [MaxLength(30)]
        public string Name { get; set; }
        [Required]
        [EmailAddress]
        [MaxLength(30)]
        public string Email { get; set; }

        public static implicit operator Roommate(CreateRoomie createRoomie)
        {
            return new Roommate
            {
                Name = createRoomie.Name,
                Email = createRoomie.Email,
                Username = createRoomie.Name.Substring(0, createRoomie.Name.IndexOf(' ')).ToLower()
            };
        }
    }
}
