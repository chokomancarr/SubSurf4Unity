import bpy
import datetime
import mathutils
import os
import struct
import sys

#Export SS data: only vertex and faces needed

class KTMExporter():
    args = sys.argv[sys.argv.index("--") + 1:]
    scene = bpy.context.scene
    timeline_markers = bpy.context.scene.timeline_markers
    obj = bpy.context.active_object
    arm = None

    # export prop
    frame_start = scene.frame_start
    frame_end = scene.frame_end
    frame_size = frame_end - frame_start + 1
    scale = 1.0
    
    meta = "KTM123"
    dir = args[0]
    name = args[1]
    path = None

    frame_offset = 0
    log = []

    def execute(self):
        if os.access(self.dir, os.W_OK) is False:
            print("permission denied : " + self.dir)
            return False
        self.path = os.path.join(self.dir, self.name)
        print ("writing to: " + self.path)

        with open(self.path, "wb") as file:
            self.write(file, self.meta + "\r\n")
            for obj in self.scene.objects:
                if obj.type != 'MESH':
                    continue;
                print ("obj " + obj.name);
                
                self.write(file, "  obj " + obj.name + " [\r\n")
                #poss = obj.location
                #self.write(file, "    pos {:f} {:f} {:f}\r\n".format(poss[0], poss[1], poss[2]))
                #rott = obj.rotation_quaternion
                #self.write(file, "    rot {:f} {:f} {:f} {:f}\r\n".format(rott[0], rott[1], rott[2], rott[3]))
                #scll = obj.scale
                #self.write(file, "    scl {:f} {:f} {:f}\r\n\r\n".format(scll[0], scll[1], scll[2]))
                for vert in obj.data.vertices:
                    self.write(file, "    vrt {} {:f} {:f} {:f}\r\n".format(vert.index, vert.co[0], vert.co[1], vert.co[2]))
                self.write(file, "\r\n")
                for poly in obj.data.polygons:
                    self.write(file, "    tri ")
                    for loop_index in poly.loop_indices:
                        self.write(file, " {}".format(obj.data.loops[loop_index].vertex_index))
                    self.write(file, "\r\n")
                self.write(file, "\r\n  ]\r\n")
        #sys.exit()

    def write (self, file, _str):
        file.write(_str.encode())

if __name__ == "__main__":
    print("----- start " + datetime.datetime.now().strftime("%H:%M:%S") + " -----")
    KTMExporter().execute()
    print("----- end   " + datetime.datetime.now().strftime("%H:%M:%S") + " -----")