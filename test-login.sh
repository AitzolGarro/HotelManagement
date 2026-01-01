#!/bin/bash

echo "Testing Hotel Reservation System Login API..."
echo "Application should be running on http://localhost:5001"
echo ""

# Test different credential combinations
echo "Testing admin@demo.com / Demo123!"
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@demo.com","password":"Demo123!"}' \
  -w "\nStatus: %{http_code}\n\n"

echo "Testing admin / password123"
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin","password":"password123"}' \
  -w "\nStatus: %{http_code}\n\n"

echo "Testing manager1@demo.com / Demo123!"
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"manager1@demo.com","password":"Demo123!"}' \
  -w "\nStatus: %{http_code}\n\n"

echo "Testing manager1 / password123"
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"manager1","password":"password123"}' \
  -w "\nStatus: %{http_code}\n\n"