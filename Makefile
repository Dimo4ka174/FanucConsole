.PHONY: help build linux win win-nodb save-images load-images backup db-info down clean

help:
	@echo "Usage:"
	@echo "  make build              - build the Docker image"
	@echo "  make linux              - start app + PostgreSQL in Docker"
	@echo "  make win [ip1 ip2]      - start PostgreSQL in Docker and run the Windows exe"
	@echo "  make win-nodb [ip1 ip2] - run the Windows exe without Docker / PostgreSQL"
	@echo "  make save-images        - save Docker images to tar"
	@echo "  make load-images        - load Docker images from tar"
	@echo "  make backup             - dump the database"
	@echo "  make db-info            - write a database report to db_report.txt"
	@echo "  make down               - stop containers"
	@echo "  make clean              - remove containers and volumes"

build:
	docker build -t fanuc-focas-console:latest .

save-images:
	docker save -o fanuc-focas-console.tar fanuc-focas-console:latest
	docker pull postgres:16-alpine
	docker save -o postgres-16-alpine.tar postgres:16-alpine
	@echo "Images saved"

load-images:
	@if [ -f fanuc-focas-console.tar ]; then docker load -i fanuc-focas-console.tar; else echo "fanuc-focas-console.tar not found, skipping"; fi
	@if [ -f postgres-16-alpine.tar ]; then docker load -i postgres-16-alpine.tar; else echo "postgres-16-alpine.tar not found, skipping"; fi

linux: load-images
	docker rm -f fanuc_postgres || true
	docker compose up -d --no-build

win: load-images
	docker rm -f fanuc_postgres || true
	docker run -d --name fanuc_postgres \
	  -e POSTGRES_USER=postgres \
	  -e POSTGRES_PASSWORD=root \
	  -e POSTGRES_DB=fanuc_data \
	  -p 5432:5432 \
	  postgres:16-alpine
	sleep 5
	dotnet publish -c Release -p:PlatformTarget=x86 -p:RuntimeIdentifier=win-x86 --self-contained true -o bin/publish/win-x86
	cp NativeLibs/Fwlib32.dll bin/publish/win-x86/ 2>/dev/null || true
	cp NativeLibs/fwlibe1.dll bin/publish/win-x86/ 2>/dev/null || true
	@if [ -n "$(filter-out $@,$(MAKECMDGOALS))" ]; then \
		./bin/publish/win-x86/FanucFocasConsole.exe $(filter-out $@,$(MAKECMDGOALS)); \
	else \
		./bin/publish/win-x86/FanucFocasConsole.exe; \
	fi

win-nodb:
	dotnet publish -c Release -p:PlatformTarget=x86 -p:RuntimeIdentifier=win-x86 --self-contained true -o bin/publish/win-x86
	cp NativeLibs/Fwlib32.dll bin/publish/win-x86/ 2>/dev/null || true
	cp NativeLibs/fwlibe1.dll bin/publish/win-x86/ 2>/dev/null || true
	@if [ -n "$(filter-out $@,$(MAKECMDGOALS))" ]; then \
		./bin/publish/win-x86/FanucFocasConsole.exe $(filter-out $@,$(MAKECMDGOALS)); \
	else \
		./bin/publish/win-x86/FanucFocasConsole.exe; \
	fi

backup:
	docker exec fanuc_postgres pg_dump -U postgres fanuc_data > backup_fanuc_data.sql
	@echo "Backup saved to backup_fanuc_data.sql"

db-info:
	@echo "=== Database report: fanuc_data ===" > db_report.txt
	@date >> db_report.txt
	@echo "" >> db_report.txt
	@echo "Tables:" >> db_report.txt
	@docker exec fanuc_postgres psql -U postgres -d fanuc_data -c "\dt" >> db_report.txt
	@echo "" >> db_report.txt
	@echo "Row counts:" >> db_report.txt
	@docker exec fanuc_postgres psql -U postgres -d fanuc_data -c "SELECT 'snapshots' AS table, COUNT(*) FROM snapshots UNION ALL SELECT 'status', COUNT(*) FROM status UNION ALL SELECT 'loads', COUNT(*) FROM loads UNION ALL SELECT 'working_time', COUNT(*) FROM working_time UNION ALL SELECT 'alarms', COUNT(*) FROM alarms;" >> db_report.txt
	@echo "" >> db_report.txt
	@echo "Latest 10 snapshots:" >> db_report.txt
	@docker exec fanuc_postgres psql -U postgres -d fanuc_data -c "SELECT id, machine_ip, timestamp FROM snapshots ORDER BY id DESC LIMIT 10;" >> db_report.txt
	@echo "" >> db_report.txt
	@echo "Latest status rows:" >> db_report.txt
	@docker exec fanuc_postgres psql -U postgres -d fanuc_data -c "SELECT * FROM status ORDER BY id DESC LIMIT 10;" >> db_report.txt
	@echo "" >> db_report.txt
	@echo "Latest loads rows:" >> db_report.txt
	@docker exec fanuc_postgres psql -U postgres -d fanuc_data -c "SELECT * FROM loads ORDER BY id DESC LIMIT 10;" >> db_report.txt
	@echo "" >> db_report.txt
	@echo "Latest working_time rows:" >> db_report.txt
	@docker exec fanuc_postgres psql -U postgres -d fanuc_data -c "SELECT * FROM working_time ORDER BY id DESC LIMIT 10;" >> db_report.txt
	@echo "" >> db_report.txt
	@echo "Latest alarms rows:" >> db_report.txt
	@docker exec fanuc_postgres psql -U postgres -d fanuc_data -c "SELECT * FROM alarms ORDER BY id DESC LIMIT 10;" >> db_report.txt
	@echo "Report saved to db_report.txt"
	@cat db_report.txt

down:
	docker compose down

clean:
	docker compose down -v
	docker rm -f fanuc_postgres || true
