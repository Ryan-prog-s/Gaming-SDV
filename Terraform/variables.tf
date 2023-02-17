#Def des variables

// Variable to define the name of the resource
variable "rg_name" {
  type        = string
  description = "Name of the resource group."
}

// Variable to define the location of the resource
variable "rg_location" {
  type        = string
  description = "Location of the resource group."
}

// Variable to define the app service plan of the resource
variable "app_service_plan_name" {
  type        = string
  description = "Name of the app service plan"
}

// Variable to define the app service os type of the resource
variable "app_service_os_type" {
  type        = string
  description = "The name of the OS of app service."
}

// Variable to define the sku of the app service
variable "app_service_sku_name" {
  type        = string
  description = "The name of the app service plan sku."
}

// Variable to define the name of the app service
variable "app_service_name" {
  type        = string
  description = "The name of the app service"
}

// Variable to define the name of the SQL Server
variable "db_server_name" {
  type        = string
  description = "The name of the database server."
}

// Variable to define the login to connect to the database
variable "db_login" {
  type        = string
  description = "The login of the database server."
}

// Variable to define the password of the database
variable "db_password" {
  type        = string
  description = "The password of the database server."
}

// Variable to define the name of the database
variable "db_name" {
  type        = string
  description = "The name of the database."
}