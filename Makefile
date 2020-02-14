bin/h1spec.exe: src/*.* src/Tests/*.* src/HTTP/*.*
	@mkdir -p bin
	mcs -out:$@ $^
