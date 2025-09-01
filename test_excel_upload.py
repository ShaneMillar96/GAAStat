#!/usr/bin/env python3
"""
Test script for uploading Excel file to GAAStat API
Tests the /api/matches/upload endpoint with the Drum Analysis 2025.xlsx file
"""

import requests
import os
import json
from datetime import datetime

# Configuration
API_BASE_URL = "http://localhost:5024"
EXCEL_FILE_PATH = "/Users/shane.millar/Downloads/Drum Analysis 2025.xlsx"
UPLOAD_ENDPOINT = f"{API_BASE_URL}/api/matches/upload"

def test_excel_upload():
    """Test uploading the Excel file to the API"""
    
    print(f"GAAStat Excel Upload Test")
    print(f"{'='*50}")
    print(f"Time: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print(f"File: {EXCEL_FILE_PATH}")
    print(f"Endpoint: {UPLOAD_ENDPOINT}")
    print(f"{'='*50}\n")
    
    # Check if file exists
    if not os.path.exists(EXCEL_FILE_PATH):
        print(f"❌ Error: Excel file not found at {EXCEL_FILE_PATH}")
        return False
    
    file_size = os.path.getsize(EXCEL_FILE_PATH)
    print(f"✓ File found: {file_size:,} bytes ({file_size/1024/1024:.2f} MB)\n")
    
    # Prepare the multipart form data
    try:
        with open(EXCEL_FILE_PATH, 'rb') as f:
            files = {
                'File': ('Drum Analysis 2025.xlsx', f, 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet')
            }
            
            # Include bulk operation configuration for optimal performance
            data = {
                'BatchSize': '1000',
                'EnableBulkInsert': 'true',
                'EnableParallelProcessing': 'true',
                'MaxConcurrentBatches': '4'
            }
            
            print("📤 Uploading Excel file...")
            print(f"   Batch Size: {data['BatchSize']}")
            print(f"   Bulk Insert: {data['EnableBulkInsert']}")
            print(f"   Parallel Processing: {data['EnableParallelProcessing']}")
            print(f"   Max Concurrent Batches: {data['MaxConcurrentBatches']}\n")
            
            # Send the request
            response = requests.post(
                UPLOAD_ENDPOINT,
                files=files,
                data=data,
                timeout=300  # 5 minute timeout for large file processing
            )
            
            # Process response
            print(f"📥 Response Status: {response.status_code}")
            
            if response.status_code == 200:
                print("✅ Upload Successful!\n")
                
                try:
                    result = response.json()
                    
                    if 'data' in result:
                        data = result['data']
                        print("📊 Import Summary:")
                        print(f"   File: {data.get('fileName', 'N/A')}")
                        print(f"   Matches Imported: {data.get('matchesImported', 0)}")
                        print(f"   Players Processed: {data.get('playersProcessed', 0)}")
                        print(f"   Statistics Records: {data.get('statisticsRecordsCreated', 0)}")
                        
                        if 'processingDuration' in data:
                            duration = data['processingDuration']
                            print(f"   Processing Time: {duration}")
                        
                        if 'validationWarnings' in data and data['validationWarnings']:
                            print(f"\n⚠️  Validation Warnings:")
                            for warning in data['validationWarnings']:
                                print(f"   - {warning}")
                    
                    print(f"\n✅ Full Response:")
                    print(json.dumps(result, indent=2))
                    
                except json.JSONDecodeError:
                    print(f"Response body: {response.text}")
                
                return True
                
            elif response.status_code == 400:
                print("❌ Bad Request\n")
                try:
                    error_data = response.json()
                    print(f"Error Message: {error_data.get('message', 'Unknown error')}")
                    
                    if 'errors' in error_data and error_data['errors']:
                        print("Validation Errors:")
                        for error in error_data['errors']:
                            print(f"   - {error}")
                except:
                    print(f"Response: {response.text}")
                    
            elif response.status_code == 500:
                print("❌ Internal Server Error\n")
                print("The server encountered an error processing the request.")
                print(f"Response: {response.text[:500]}...")
                
            else:
                print(f"❌ Unexpected status code: {response.status_code}")
                print(f"Response: {response.text[:500]}...")
            
            return False
            
    except requests.exceptions.Timeout:
        print("❌ Request timed out after 5 minutes")
        return False
        
    except requests.exceptions.ConnectionError as e:
        print(f"❌ Failed to connect to API: {e}")
        print("\nPlease ensure the API is running at http://localhost:5024")
        return False
        
    except Exception as e:
        print(f"❌ Unexpected error: {e}")
        return False

def test_api_health():
    """Test if the API is accessible"""
    try:
        print("🔍 Checking API health...")
        response = requests.get(f"{API_BASE_URL}/api/matches", timeout=5)
        if response.status_code in [200, 500]:  # 500 might be expected if no data
            print(f"✓ API is reachable (status: {response.status_code})\n")
            return True
        else:
            print(f"⚠️  API returned status: {response.status_code}\n")
            return True
    except Exception as e:
        print(f"❌ Cannot reach API: {e}\n")
        return False

if __name__ == "__main__":
    print("\n" + "="*60)
    print(" GAA Statistics Excel Import Test ")
    print("="*60 + "\n")
    
    # First check if API is accessible
    if test_api_health():
        # Proceed with upload test
        success = test_excel_upload()
        
        if success:
            print("\n" + "="*60)
            print(" ✅ TEST COMPLETED SUCCESSFULLY")
            print("="*60)
            print("\nNext steps:")
            print("1. Query the database to verify data was loaded")
            print("2. Check matches table for 8 match records")
            print("3. Check match_player_stats table for player statistics")
            print("4. Check import_history table for audit trail")
        else:
            print("\n" + "="*60)
            print(" ❌ TEST FAILED")
            print("="*60)
            print("\nTroubleshooting:")
            print("1. Check API logs for detailed error messages")
            print("2. Verify database connection is configured")
            print("3. Ensure Excel file format matches expected structure")
            print("4. Check that all required services are registered in DI container")
    else:
        print("Please start the API server and try again.")
        print("Run: cd backend/src/GAAStat.Api && dotnet run")