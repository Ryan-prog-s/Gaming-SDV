﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace WebGamingSDV.Models
{
    public class VirtualMachine
    {

        /// <summary>
        /// Name of the virtual machine
        /// </summary>
        [DisplayName("Name")]
        public string name { get; set; }
        /// <summary>
        /// Public IP of the virtual machine
        /// </summary>
        [Required]
        [DisplayName("Public IP")]
        public string publicIp { get; set; }
        /// <summary>
        /// Login to the virtual machine
        /// </summary>
        [Required]
        [DisplayName("Login")]
        public string login { get; set; }
        /// <summary>
        /// Password to the virtual machine
        /// </summary>
        [Required]
        [DisplayName("Password")]
        public string password { get; set; }
        public VirtualMachine(string name, string publicIp, string login, string password)
        {
            this.name = name;
            this.publicIp = publicIp;
            this.login = login;
            this.password = password;
        }
    }
}


