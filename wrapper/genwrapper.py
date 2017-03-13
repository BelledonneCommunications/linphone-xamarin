#!/usr/bin/python

# Copyright (C) 2017 Belledonne Communications SARL
# 
# This program is free software; you can redistribute it and/or
# modify it under the terms of the GNU General Public License
# as published by the Free Software Foundation; either version 2
# of the License, or (at your option) any later version.
# 
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
# 
# You should have received a copy of the GNU General Public License
# along with this program; if not, write to the Free Software
# Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

import argparse
import os
import sys
import pystache

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..', 'tools'))
print sys.path
import genapixml as CApi
import abstractapi as AbsApi

class CsharpTranslator(object):
	def __init__(self):
		self.ignore = []

	def translate_method_name(self, name, recursive=False, topAncestor=None):
		translatedName = name.to_camel_case(lower=True)
			
		if name.prev is None or not recursive or name.prev is topAncestor:
			return translatedName

	def translate_argument_name(self, name):
		argname = name.to_camel_case(lower=True)
		arg = argname
		if argname == "params":
			arg = "parameters"
		elif argname == "event":
			arg = "ev"
		elif argname == "ref":
			arg = "reference"
		elif argname == "value":
			arg = "val"
		return arg

	def translate_base_type(self, _type, isArg, dllImport=True):
		if _type.name == 'void':
				if _type.isref:
					return 'IntPtr'
				return 'void'
		elif _type.name == 'boolean':
			if dllImport:
				res = 'int' # In C the bool_t is an integer
			else:
				res = 'bool'
		elif _type.name == 'integer':
			if _type.isUnsigned:
				res = 'uint'
			else:
				res = 'int'
		elif _type.name == 'string':
			if dllImport:
				if isArg:
					return 'string'
				else:
					res = 'IntPtr' # Return as IntPtr and get string with Marshal.PtrToStringAnsi()
			else:
				return 'string'
		elif _type.name == 'character':
			if _type.isUnsigned:
				res = 'byte'
			else:
				res = 'sbyte'
		elif _type.name == 'time':
			res = 'long' #TODO check
		elif _type.name == 'size':
			res = 'long' #TODO check
		elif _type.name == 'floatant':
			return 'float'
		elif _type.name == 'string_array':
			return 'string[]'
		else:
			raise AbsApi.Error('\'{0}\' is not a base abstract type'.format(_type.name))

		return res
	
	def translate_type(self, _type, isArg, dllImport=True):
		if type(_type) is AbsApi.EnumType:
			if dllImport and isArg:
				return 'int'
			return _type.name
		elif type(_type) is AbsApi.ClassType:
			return "IntPtr" if dllImport else _type.name
		elif type(_type) is AbsApi.BaseType:
			return self.translate_base_type(_type, isArg, dllImport)
		elif type(_type) is AbsApi.ListType:
			raise AbsApi.Error('Lists are not supported right now') #TODO
	
	def translate_argument(self, arg, dllImport=True):
		return '{0} {1}'.format(self.translate_type(arg.type, isArg=True, dllImport=dllImport), self.translate_argument_name(arg.name))

	def translate_method(self, method, static=False, genImpl=True):
		if method.name.to_snake_case(fullName=True) in self.ignore:
			raise AbsApi.Error('{0} has been escaped'.format(method.name.to_snake_case(fullName=True)))

		methodElems = {}
		methodElems['return'] = self.translate_type(method.returnType, False)
		methodElems['name'] = method.name.to_c()
		methodElems['params'] = '' if static else 'IntPtr thiz'
		for arg in method.args:
			if arg is not method.args[0] or not static:
				methodElems['params'] += ', '
			methodElems['params'] += self.translate_argument(arg)
		
		methodDict = {}
		methodDict['prototype'] = """[DllImport(LinphoneWrapper.LIB_NAME)]
		static extern {return} {name}({params});""".format(**methodElems)

		methodDict['has_impl'] = genImpl
		if genImpl:
			methodDict['impl'] = {}
			methodDict['impl']['static'] = 'static ' if static else ''
			methodDict['impl']['type'] = self.translate_type(method.returnType, False, False)
			methodDict['impl']['name'] = method.name.to_camel_case()
			methodDict['impl']['return'] = '' if methodDict['impl']['type'] == "void" else 'return '
			methodDict['impl']['c_name'] = method.name.to_c()
			methodDict['impl']['nativePtr'] = '' if static else ('nativePtr, ' if len(method.args) > 0 else 'nativePtr')
			methodDict['is_string'] = methodDict['impl']['type'] == "string"
			methodDict['is_bool'] = methodDict['impl']['type'] == "bool"
			methodDict['is_class'] = methodDict['impl']['type'][:8] == "Linphone" and type(method.returnType) is AbsApi.ClassType
			methodDict['is_enum'] = methodDict['impl']['type'][:8] == "Linphone" and type(method.returnType) is AbsApi.EnumType
			methodDict['is_generic'] = not methodDict['is_string'] and not methodDict['is_bool'] and not methodDict['is_class'] and not methodDict['is_enum']
			
			methodDict['impl']['args'] = ''
			methodDict['impl']['c_args'] = ''
			for arg in method.args:
				if arg is not method.args[0]:
					methodDict['impl']['args'] += ', '
					methodDict['impl']['c_args'] += ', '
				if self.translate_type(arg.type, False, False)[:8] == "Linphone":
					if type(arg.type) is AbsApi.ClassType:
						methodDict['impl']['c_args'] += self.translate_argument_name(arg.name) + ".nativePtr"
					else:
						methodDict['impl']['c_args'] += '(int)' + self.translate_argument_name(arg.name)
				elif self.translate_type(arg.type, False, False) == "bool":
					methodDict['impl']['c_args'] += self.translate_argument_name(arg.name) + " ? 1 : 0"
				else:
					methodDict['impl']['c_args'] += self.translate_argument_name(arg.name)
				methodDict['impl']['args'] += self.translate_argument(arg, False)

		return methodDict
	
###########################################################################################################################################

	def translate_property_getter(self, prop, name, static=False):
		methodDict = self.translate_method(prop, static, False)

		methodElems = {}
		methodElems['static'] = 'static ' if static else ''
		methodElems['return'] = self.translate_type(prop.returnType, False, False)
		methodElems['name'] = (name[3:] if len(name) > 3 else 'Instance') if name[:3] == "Get" else name

		methodDict['has_property'] = True
		methodDict['property'] = "{static}public {return} {name}".format(**methodElems)
		methodDict['has_getter'] = True
		methodDict['has_setter'] = False
		methodDict['return'] = methodElems['return']
		methodDict['getter_nativePtr'] = '' if static else 'nativePtr'
		methodDict['getter_c_name'] = prop.name.to_c()
		methodDict['is_string'] = methodElems['return'] == "string"
		methodDict['is_bool'] = methodElems['return'] == "bool"
		methodDict['is_class'] = methodElems['return'][:8] == "Linphone" and type(prop.returnType) is AbsApi.ClassType
		methodDict['is_enum'] = methodElems['return'][:8] == "Linphone" and type(prop.returnType) is AbsApi.EnumType
		methodDict['is_generic'] = not methodDict['is_string'] and not methodDict['is_bool'] and not methodDict['is_class'] and not methodDict['is_enum']

		return methodDict

	def translate_property_setter(self, prop, name, static=False):
		methodDict = self.translate_method(prop, static, False)

		methodElems = {}
		methodElems['static'] = 'static ' if static else ''
		methodElems['return'] = self.translate_type(prop.args[0].type, True, False)
		methodElems['name'] = (name[3:] if len(name) > 3 else 'Instance') if name[:3] == "Get" else name

		methodDict['has_property'] = True
		methodDict['property'] = "{static}public {return} {name}".format(**methodElems)
		methodDict['has_getter'] = False
		methodDict['has_setter'] = True
		methodDict['return'] = methodElems['return']
		methodDict['setter_nativePtr'] = '' if static else 'nativePtr, '
		methodDict['setter_c_name'] = prop.name.to_c()
		methodDict['is_string'] = methodElems['return'] == "string"
		methodDict['is_bool'] = methodElems['return'] == "bool"
		methodDict['is_class'] = methodElems['return'][:8] == "Linphone" and type(prop.args[0].type) is AbsApi.ClassType
		methodDict['is_enum'] = methodElems['return'][:8] == "Linphone" and type(prop.args[0].type) is AbsApi.EnumType
		methodDict['is_generic'] = not methodDict['is_string'] and not methodDict['is_bool'] and not methodDict['is_class'] and not methodDict['is_enum']

		return methodDict

	def translate_property_getter_setter(self, prop, name, static=False):
		methodDict = self.translate_property_getter(prop.getter, name, static)
		methodDictSet = self.translate_property_setter(prop.setter, name, static)

		protoElems = {}
		protoElems['getter_prototype'] = methodDict['prototype']
		protoElems['setter_prototype'] = methodDictSet['prototype']
		methodDict["prototype"] = """{getter_prototype}
		{setter_prototype}""".format(**protoElems)

		methodDict['has_setter'] = True
		methodDict['setter_nativePtr'] = methodDictSet['setter_nativePtr']
		methodDict['setter_c_name'] = methodDictSet['setter_c_name']

		return methodDict
	
	def translate_property(self, prop):
		res = []
		name = prop.name.to_camel_case()
		if prop.getter is not None:
			if prop.setter is not None:
				res.append(self.translate_property_getter_setter(prop, name))
			else:
				res.append(self.translate_property_getter(prop.getter, name))
		elif prop.setter is not None:
			res.append(self.translate_property_setter(prop.setter, name))
		return res
	
###########################################################################################################################################

	def translate_enum(self, enum):
		enumDict = {}
		enumDict['enumName'] = "Linphone" + enum.name.to_camel_case()
		enumDict['values'] = []
		i = 0
		for enumValue in enum.values:
			enumValDict = {}
			enumValDict['name'] = enumValue.name.to_camel_case()
			enumValDict['notLast'] = i < len(enum.values) - 1
			enumDict['values'].append(enumValDict)
			i += 1
		return enumDict

	def translate_class(self, _class):
		if _class.name.to_camel_case(fullName=True) in self.ignore:
			raise AbsApi.Error('{0} has been escaped'.format(_class.name.to_camel_case(fullName=True)))

		classDict = {}
		classDict['className'] = "Linphone" + _class.name.to_camel_case()

		classDict['dllImports'] = []
		
		for method in _class.classMethods:
			try:
				if 'get' in method.name.to_word_list():
					methodDict = self.translate_property_getter(method, method.name.to_camel_case(), True)
				elif 'set' in method.name.to_word_list():
					methodDict = self.translate_property_setter(method, method.name.to_camel_case(), True)
				else:
					methodDict = self.translate_method(method, static=True, genImpl=True)
				classDict['dllImports'].append(methodDict)
			except AbsApi.Error as e:
				print('Could not translate {0}: {1}'.format(method.name.to_snake_case(fullName=True), e.args[0]))

		for prop in _class.properties:
			try:
				classDict['dllImports'] += self.translate_property(prop)
			except AbsApi.Error as e:
				print('error while translating {0} property: {1}'.format(prop.name.to_snake_case(), e.args[0]))

		for method in _class.instanceMethods:
			try:
				methodDict = self.translate_method(method, static=False, genImpl=True)
				classDict['dllImports'].append(methodDict)
			except AbsApi.Error as e:
				print('Could not translate {0}: {1}'.format(method.name.to_snake_case(fullName=True), e.args[0]))

		return classDict

	def translate_interface(self, interface):
		if interface.name.to_camel_case(fullName=True) in self.ignore:
			raise AbsApi.Error('{0} has been escaped'.format(interface.name.to_camel_case(fullName=True)))

		classDict = {}
		classDict['interfaceName'] = "Linphone" + interface.name.to_camel_case()

		return classDict

###########################################################################################################################################

class EnumImpl(object):
	def __init__(self, enum, translator):
		namespace = enum.find_first_ancestor_by_type(AbsApi.Namespace)
		self.namespace = namespace.name.concatenate(fullName=True) if namespace is not None else None
		self.enum = translator.translate_enum(enum)

class ClassImpl(object):
	def __init__(self, _class, translator):
		namespace = _class.find_first_ancestor_by_type(AbsApi.Namespace)
		self.namespace = namespace.name.concatenate(fullName=True) if namespace is not None else None
		self._class = translator.translate_class(_class)

class InterfaceImpl(object):
	def __init__(self, interface, translator):
		namespace = interface.find_first_ancestor_by_type(AbsApi.Namespace)
		self.namespace = namespace.name.concatenate(fullName=True) if namespace is not None else None
		self.interface = translator.translate_interface(interface)

class WrapperImpl(object):
	def __init__(self, enums, interfaces, classes):
		self.enums = enums
		self.interfaces = interfaces
		self.classes = classes
	
###########################################################################################################################################

def render(renderer, item, path):
	tmppath = path + '.tmp'
	content = ''
	with open(tmppath, mode='w') as f:
		f.write(renderer.render(item))
	with open(tmppath, mode='rU') as f:
		content = f.read()
	with open(path, mode='w') as f:
		f.write(content)
	os.unlink(tmppath)

def main():
	argparser = argparse.ArgumentParser(description='Generate source files for the C++ wrapper')
	argparser.add_argument('xmldir', type=str, help='Directory where the XML documentation of the Linphone\'s API generated by Doxygen is placed')
	argparser.add_argument('-o --output', type=str, help='the directory where to generate the source files', dest='outputdir', default='.')
	argparser.add_argument('-n --name', type=str, help='the name of the genarated source file', dest='outputfile', default='LinphoneWrapper.cs')
	args = argparser.parse_args()
	
	entries = os.listdir(args.outputdir)
	
	project = CApi.Project()
	project.initFromDir(args.xmldir)
	project.check()
	
	parser = AbsApi.CParser(project)
	parser.parse_all()
	translator = CsharpTranslator()
	renderer = pystache.Renderer()
	
	enums = []
	for item in parser.enumsIndex.items():
		if item[1] is not None:
			impl = EnumImpl(item[1], translator)
			enums.append(impl)
		else:
			print('warning: {0} enum won\'t be translated because of parsing errors'.format(item[0]))

	interfaces = []
	classes = []
	for _class in parser.classesIndex.values() + parser.interfacesIndex.values():
		if _class is not None:
			try:
				if type(_class) is AbsApi.Class:
					impl = ClassImpl(_class, translator)
					classes.append(impl)
				else:
					impl = InterfaceImpl(_class, translator)
					interfaces.append(impl)
			except AbsApi.Error as e:
				print('Could not translate {0}: {1}'.format(_class.name.to_camel_case(fullName=True), e.args[0]))

	wrapper = WrapperImpl(enums, interfaces, classes)
	render(renderer, wrapper, args.outputdir + "/" + args.outputfile)

if __name__ == '__main__':
	main()