#Def ressources Azure
#   => terraform version
#   => providers Azure

// Include version of Terrfaorme
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "3.43.0"
    }
  }
  required_version = "1.3.7"
}

// Credentials of Azure account
provider "azurerm" {
  features {
  }
  subscription_id = "6d8bbd02-57ea-4135-b49c-a828ad4bea9f"
  tenant_id       = "b7b023b8-7c32-4c02-92a6-c8cdaa1d189c"
}

// Create a new resource group
resource "azurerm_resource_group" "gaming_1" {
  name     = var.rg_name
  location = var.rg_location
}

// Create App Plan for the resource group
resource "azurerm_service_plan" "gaming_1" {
  name                = var.app_service_plan_name
  location            = azurerm_resource_group.gaming_1.location
  resource_group_name = azurerm_resource_group.gaming_1.name
  os_type             = var.app_service_os_type
  sku_name            = var.app_service_sku_name
}

// App Website for the resource group
resource "azurerm_windows_web_app" "gaming_1" {
  name                = var.app_service_name
  resource_group_name = azurerm_resource_group.gaming_1.name
  location            = azurerm_resource_group.gaming_1.location
  service_plan_id     = azurerm_service_plan.gaming_1.id

  site_config {
    application_stack {
      current_stack  = "dotnet"
      dotnet_version = "v7.0"
    }
  }
}

// Referencee the SQL Server for the database
resource "azurerm_mssql_server" "gaming_1" {
  name                         = var.db_server_name
  resource_group_name          = azurerm_resource_group.gaming_1.name
  location                     = azurerm_service_plan.gaming_1.location
  version                      = "12.0"
  administrator_login          = var.db_login
  administrator_login_password = var.db_password

  tags = {
    environment = "production"
  }
}

// Reference the SQL Database
resource "azurerm_mssql_database" "db_vm" {
  name           = var.db_name
  server_id      = azurerm_mssql_server.gaming_1.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  license_type   = "LicenseIncluded"
  max_size_gb    = 1
  read_scale     = false
  sku_name       = "S0"
  zone_redundant = false
}